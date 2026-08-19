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
/// Esseni OpenRouter üzərindən qiymətləndirir. Qiymətləndirmə iki ardıcıl çağırışdan ibarətdir:
/// əvvəlcə səhvlər tapılır, sonra həmin siyahı giriş kimi verilib bal və müəllim rəyi alınır.
/// Hər çağırış model uğursuz olarsa ehtiyat modelə keçir. Cavabın parse edilməsi və domenə
/// çevrilməsi ayrıca siniflərə həvalə olunub (<see cref="AiEssayResponseParser"/>,
/// <see cref="EssayEvaluationMapper"/>).
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
        var hasImages = promptImages is { Count: > 0 };
        var userContent = BuildUserContent(essayText, promptImages);

        var detection = await CallWithFallbackAsync<AiDetectionResponse>(
            EssayPrompts.DetectionRules,
            EssayPrompts.GetDetectionInput(hasImages),
            userContent,
            EssaySchemas.Detection,
            cancellationToken);

        if (!detection.IsEssay)
        {
            return new EssayEvaluationData
            {
                IsEssay = false,
                InvalidReason = "The submitted text is not an essay."
            };
        }

        var mistakes = EssayEvaluationMapper.BuildMistakes(detection.Mistakes, essayText);

        // Bal çağırışı səhv siyahısından asılıdır, ona görə paralel deyil, ardıcıl gedir:
        // qrammatika balı real səhv sıxlığını əks etdirməlidir, model isə səhvləri ikinci
        // dəfə axtarmamalıdır.
        var scoring = await CallWithFallbackAsync<AiScoringResponse>(
            EssayPrompts.ScoringRules,
            EssayPrompts.GetScoringInput(grade, essayText, topic, mistakes.Mistakes, hasImages),
            userContent,
            EssaySchemas.Scoring,
            cancellationToken);

        return EssayEvaluationMapper.Map(mistakes, scoring, grade, essayText);
    }

    /// <summary>
    /// Sırayla cəhd ediləcək modellər: əvvəlcə əsas, o uğursuz olarsa (və konfiqurasiya olunubsa)
    /// bir dəfə ehtiyat model. Siyahı sabit və ən çoxu 2 elementdir — buna görə aşağıdakı dövr
    /// struktur olaraq sonsuz loopa düşə bilməz.
    /// </summary>
    private async Task<T> CallWithFallbackAsync<T>(
        string staticRules,
        string dynamicInput,
        object userContent,
        object responseSchema,
        CancellationToken cancellationToken) where T : class
    {
        var messages = BuildMessages(staticRules, dynamicInput, userContent);

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
                var raw = await _client.CompleteAsync(model, messages, cancellationToken, responseSchema);
                var parsed = AiEssayResponseParser.Parse<T>(raw);

                if (i > 0)
                {
                    _logger.LogWarning(
                        "Əsas model uğursuz olduğu üçün ehtiyat modelə ({Model}) yönləndirildi və uğurlu oldu (bu cəhd {ElapsedMs}ms, cəmi {TotalMs}ms).",
                        model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds, stopwatch.ElapsedMilliseconds);
                }

                return parsed;
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
    /// dəsti cache_control ilə işarələnir və HƏR sorğuda (sinif/mövzu/esse fərqli olsa belə)
    /// bayt-bayt eynidir, ona görə Anthropic onu keşləyir. Sorğuya-görə-dəyişən dəyərlər ayrı,
    /// keşlənməmiş bir bloka qoyulur.
    /// </summary>
    private static ChatMessage[] BuildMessages(string staticRules, string dynamicInput, object userContent) =>
    [
        new ChatMessage
        {
            Role = "system",
            Content = new[]
            {
                new TextContentPart { Text = staticRules, CacheControl = new CacheControl() },
                new TextContentPart { Text = dynamicInput }
            }
        },
        new ChatMessage { Role = "user", Content = userContent }
    ];

    /// <summary>
    /// Şəkil yoxdursa (11-ci sinif) sadə mətn stringi, varsa (9-cu sinif) mətn + şəkillərdən
    /// ibarət multimodal content massivi qaytarır — vision dəstəkli model hər ikisini eyni
    /// sorğuda görür.
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
