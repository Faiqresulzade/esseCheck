using System.Diagnostics;
using System.Text.Json;
using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace EssayChecker.Infrastructure.Services.Essays;

internal sealed class OpenRouterEssayEvaluator : IEssayEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OpenRouterClient _client;
    private readonly Application.Settings.OpenRouterSettings _settings;
    private readonly ILogger<OpenRouterEssayEvaluator> _logger;

    public OpenRouterEssayEvaluator(
        OpenRouterClient client,
        Microsoft.Extensions.Options.IOptions<Application.Settings.OpenRouterSettings> options,
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

        // Sistem promptu iki hissəyə bölünür ki, Anthropic prompt caching işləsin: sabit qayda
        // dəsti (StaticRules) cache_control ilə işarələnir və HƏR sorğuda (sinif/mövzu/esse
        // fərqli olsa belə) bayt-bayt eynidir, ona görə Anthropic onu keşləyir. Sorğuya-görə-
        // dəyişən dəyərlər (sinif, mövzu, söz sayı) ayrı, keşlənməmiş bir bloka qoyulur.
        var messages = new[]
        {
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
        };

        // Sırayla cəhd ediləcək modellər: əvvəlcə pulsuz, o uğursuz olarsa (və konfiqurasiya
        // olunubsa) bir dəfə pullu ehtiyat model. Siyahı sabit və ən çoxu 2 elementdir —
        // buna görə aşağıdakı foreach struktur olaraq sonsuz loopa düşə bilməz.
        var modelsToTry = string.IsNullOrWhiteSpace(_settings.FallbackModel)
            ? new[] { _settings.Model }
            : new[] { _settings.Model, _settings.FallbackModel };

        Exception? lastError = null;
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < modelsToTry.Length; i++)
        {
            var model = modelsToTry[i];
            var isFallback = i > 0;
            var attemptStart = stopwatch.Elapsed;
            try
            {
                var raw = await _client.CompleteAsync(model, messages, cancellationToken);
                var dto = ParseDto(raw);

                if (isFallback)
                {
                    _logger.LogWarning(
                        "Pulsuz model uğursuz olduğu üçün pullu ehtiyat modelə ({Model}) yönləndirildi və uğurlu oldu (bu cəhd {ElapsedMs}ms, cəmi {TotalMs}ms).",
                        model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds, stopwatch.ElapsedMilliseconds);
                }

                return MapToResult(dto, grade, essayText);
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

    private AiEssayDto ParseDto(string raw)
    {
        var json = ExtractJson(raw);
        var dto = JsonSerializer.Deserialize<AiEssayDto>(json, JsonOptions);
        return dto ?? throw new AiServiceException("AI cavabı boş qayıtdı.", isTransient: true);
    }

    /// <summary>11-ci sinifdə bu həddən az söz varsa, content balı istisnasız 0 olmalıdır (AI-a etibar edilmir).</summary>
    private const int Grade11ContentZeroFloorWords = 70;

    private static EssayEvaluationData MapToResult(AiEssayDto dto, GradeLevel grade, string essayText)
    {
        if (string.Equals(dto.Status, "invalid", StringComparison.OrdinalIgnoreCase))
        {
            return new EssayEvaluationData
            {
                IsEssay = false,
                InvalidReason = dto.Reason ?? "The submitted text is not an essay."
            };
        }

        var mistakes = MapMistakes(dto.Mistakes);
        var scores = MapScores(dto.Scores);

        // AI-ın verdiyi bala etibar etmirik — 11-ci sinifdə 70 sözdən az essedə content balı
        // kodla məcburi 0-a endirilir, AI nə qaytarsa qaytarsın (istənilən halda).
        if (grade == GradeLevel.Grade11 && EssayPrompts.CountWords(essayText) < Grade11ContentZeroFloorWords)
        {
            var total = scores.Structure + 0 + scores.Grammar + scores.Vocabulary;
            scores = scores with
            {
                Content = 0,
                ContentComment = "70 sözdən az yazılmış esselərdə (11-ci sinif) məzmun balı avtomatik 0 təyin olunur.",
                Total = total
            };
        }

        return new EssayEvaluationData
        {
            IsEssay = true,
            CorrectedEssay = dto.CorrectedEssay ?? string.Empty,
            // AI-ın öz-özünə saydığı "statistics" tez-tez mistakes massivinin faktiki tərkibi
            // ilə uyğun gəlmir (bir neçə model üzərində test edilib, hamısında rast gəlindi) —
            // ona görə etibar etmək əvəzinə, statistikanı birbaşa artıq map olunmuş mistakes
            // siyahısından özümüz sayırıq. Bu, 100% uyğunluğu zəmanət altına alır.
            Statistics = ComputeStatistics(mistakes),
            Scores = scores,
            Mistakes = mistakes,
            Feedback = MapFeedback(dto.TeacherFeedback)
        };
    }

    /// <summary>Sistem promptu xam JSON tələb edir, amma ehtiyat üçün ``` bloklarını təmizləyirik.</summary>
    private static string ExtractJson(string content)
    {
        var text = content.Trim();

        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = text.IndexOf('\n');
            if (firstNewLine >= 0)
                text = text[(firstNewLine + 1)..];
            if (text.EndsWith("```", StringComparison.Ordinal))
                text = text[..^3];
            text = text.Trim();
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }

    private static EssayStatisticsDto ComputeStatistics(IReadOnlyList<EssayMistakeDto> mistakes)
    {
        var grammar = 0;
        var spelling = 0;
        var vocabulary = 0;
        var naturalExpression = 0;

        foreach (var m in mistakes)
        {
            switch (m.Category)
            {
                case MistakeCategory.Grammar: grammar++; break;
                case MistakeCategory.Spelling: spelling++; break;
                case MistakeCategory.Vocabulary: vocabulary++; break;
                case MistakeCategory.NaturalExpression: naturalExpression++; break;
            }
        }

        return new EssayStatisticsDto(grammar, spelling, vocabulary, naturalExpression, mistakes.Count);
    }

    private static EssayScoresDto MapScores(AiScores? s) =>
        s is null
            ? new EssayScoresDto(0, "", 0, "", 0, "", 0, "", 0)
            : new EssayScoresDto(
                s.Structure, s.StructureComment ?? "",
                s.Content, s.ContentComment ?? "",
                s.Grammar, s.GrammarComment ?? "",
                s.Vocabulary, s.VocabularyComment ?? "",
                s.Total);

    private static IReadOnlyList<EssayMistakeDto> MapMistakes(List<AiMistake>? mistakes)
    {
        if (mistakes is null || mistakes.Count == 0)
            return Array.Empty<EssayMistakeDto>();

        var result = new List<EssayMistakeDto>(mistakes.Count);
        foreach (var m in mistakes)
        {
            var category = Enum.TryParse<MistakeCategory>(m.Category, ignoreCase: true, out var parsed)
                ? parsed
                : MistakeCategory.Grammar;

            result.Add(new EssayMistakeDto(m.Wrong ?? string.Empty, m.Correct ?? string.Empty, category, m.Reason ?? string.Empty));
        }

        return result;
    }

    private static TeacherFeedbackDto MapFeedback(AiFeedback? f) =>
        f is null
            ? new TeacherFeedbackDto(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>())
            : new TeacherFeedbackDto(
                f.Strengths ?? new List<string>(),
                f.Weaknesses ?? new List<string>(),
                f.Recommendations ?? new List<string>());

    // --- AI JSON parse modelləri ---

    private sealed class AiEssayDto
    {
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? CorrectedEssay { get; set; }
        public AiStatistics? Statistics { get; set; }
        public List<AiMistake>? Mistakes { get; set; }
        public AiScores? Scores { get; set; }
        public AiFeedback? TeacherFeedback { get; set; }
    }

    private sealed class AiStatistics
    {
        public int Grammar { get; set; }
        public int Spelling { get; set; }
        public int Vocabulary { get; set; }
        public int NaturalExpression { get; set; }
        public int Total { get; set; }
    }

    private sealed class AiMistake
    {
        public string? Wrong { get; set; }
        public string? Correct { get; set; }
        public string? Category { get; set; }
        public string? Reason { get; set; }
    }

    private sealed class AiScores
    {
        public double Structure { get; set; }
        public string? StructureComment { get; set; }
        public double Content { get; set; }
        public string? ContentComment { get; set; }
        public double Grammar { get; set; }
        public string? GrammarComment { get; set; }
        public double Vocabulary { get; set; }
        public string? VocabularyComment { get; set; }
        public double Total { get; set; }
    }

    private sealed class AiFeedback
    {
        public List<string>? Strengths { get; set; }
        public List<string>? Weaknesses { get; set; }
        public List<string>? Recommendations { get; set; }
    }
}
