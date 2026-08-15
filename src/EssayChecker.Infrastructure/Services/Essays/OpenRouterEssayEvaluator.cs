using System.Diagnostics;
using System.Text.Json;
using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>
/// Esseni OpenRouter üzərindən qiymətləndirir: sorğunu qurur, model uğursuz olarsa ehtiyat
/// modelə keçir. Cavabın parse edilməsi və domenə çevrilməsi ayrıca siniflərə həvalə olunub
/// (<see cref="AiEssayResponseParser"/>, <see cref="EssayEvaluationMapper"/>).
/// </summary>
internal sealed class OpenRouterEssayEvaluator : IEssayEvaluator
{
    private readonly OpenRouterClient _client;
    private readonly OpenRouterSettings _settings;
    private readonly ILogger<OpenRouterEssayEvaluator> _logger;

    public OpenRouterEssayEvaluator(
        OpenRouterClient client,
        IOptions<OpenRouterSettings> options,
        ILogger<OpenRouterEssayEvaluator> logger)
    {
        _client = client;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<EssayEvaluationData> EvaluateAsync(
        string essayText,
        GradeLevel grade,
        string? topic,
        IReadOnlyList<PromptImage>? promptImages = null,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(essayText, grade, topic, promptImages);

        // Sırayla cəhd ediləcək modellər: əvvəlcə əsas, o uğursuz olarsa (və konfiqurasiya
        // olunubsa) bir dəfə ehtiyat model. Siyahı sabit və ən çoxu 2 elementdir — buna görə
        // aşağıdakı dövr struktur olaraq sonsuz loopa düşə bilməz.
        var modelsToTry = string.IsNullOrWhiteSpace(_settings.FallbackModel)
            ? new[] { _settings.Model }
            : new[] { _settings.Model, _settings.FallbackModel };

        Exception? lastError = null;
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < modelsToTry.Length; i++)
        {
            var model = modelsToTry[i];
            var attemptStart = stopwatch.Elapsed;
            try
            {
                var raw = await _client.CompleteAsync(model, messages, cancellationToken);
                var dto = AiEssayResponseParser.Parse(raw);

                if (i > 0)
                {
                    _logger.LogWarning(
                        "Əsas model uğursuz olduğu üçün ehtiyat modelə ({Model}) yönləndirildi və uğurlu oldu (bu cəhd {ElapsedMs}ms, cəmi {TotalMs}ms).",
                        model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds, stopwatch.ElapsedMilliseconds);
                }

                return EssayEvaluationMapper.Map(dto, grade, essayText);
            }
            catch (JsonException ex)
            {
                lastError = ex;
                _logger.LogWarning(ex,
                    "OpenRouter cavabı JSON parse edilmədi (model {Model}, bu cəhd {ElapsedMs}ms).",
                    model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds);
            }
            catch (AiServiceException ex) when (ex.IsTransient)
            {
                lastError = ex;
                _logger.LogWarning(ex,
                    "OpenRouter keçici xəta verdi (model {Model}, bu cəhd {ElapsedMs}ms).",
                    model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds);
            }
            // Transient olmayan AiServiceException (məs. konfiqurasiya xətası) burada tutulmur —
            // ehtiyat modelə keçmək mənasızdır, dərhal yuxarı ötürülür.
        }

        // Bura yalnız bütün modellər JsonException/keçici xəta ilə bitəndə çatılır.
        // Həmişə AiServiceException atırıq ki, GlobalExceptionHandler düzgün (503) cavab versin.
        throw new AiServiceException(
            $"AI qiymətləndirməsi bütün modellərdən ({string.Join(", ", modelsToTry)}) sonra uğursuz oldu: {lastError?.Message}",
            isTransient: true,
            innerException: lastError);
    }

    /// <summary>
    /// Sistem promptu iki hissəyə bölünür ki, Anthropic prompt caching işləsin: sabit qayda
    /// dəsti (StaticRules) cache_control ilə işarələnir və HƏR sorğuda (sinif/mövzu/esse fərqli
    /// olsa belə) bayt-bayt eynidir, ona görə Anthropic onu keşləyir. Sorğuya-görə-dəyişən
    /// dəyərlər (sinif, mövzu, söz sayı) ayrı, keşlənməmiş bir bloka qoyulur.
    /// </summary>
    private static ChatMessage[] BuildMessages(
        string essayText,
        GradeLevel grade,
        string? topic,
        IReadOnlyList<PromptImage>? promptImages)
    {
        var hasImages = promptImages is { Count: > 0 };

        return
        [
            new ChatMessage
            {
                Role = "system",
                Content = new[]
                {
                    new TextContentPart
                    {
                        Text = EssayPrompts.StaticRules,
                        CacheControl = new CacheControl()
                    },
                    new TextContentPart
                    {
                        Text = EssayPrompts.GetDynamicInputVariables(grade, essayText, topic, hasImages)
                    }
                }
            },
            new ChatMessage { Role = "user", Content = BuildUserContent(essayText, promptImages) }
        ];
    }

    /// <summary>
    /// Şəkil yoxdursa (11-ci sinif) sadə mətn stringi, varsa (9-cu sinif) mətn + şəkillərdən
    /// ibarət multimodal content massivi qaytarır — vision dəstəkli model (hazırda gpt-4o-mini)
    /// hər ikisini eyni sorğuda görür.
    /// </summary>
    private static object BuildUserContent(string essayText, IReadOnlyList<PromptImage>? promptImages)
    {
        if (promptImages is not { Count: > 0 })
            return essayText;

        var parts = new List<TextOrImageContentPart> { new() { Type = "text", Text = essayText } };
        parts.AddRange(promptImages.Select(img => new TextOrImageContentPart
        {
            Type = "image_url",
            ImageUrl = new ImageUrlPart { Url = $"data:{img.ContentType};base64,{Convert.ToBase64String(img.Data)}" }
        }));

        return parts;
    }
}
