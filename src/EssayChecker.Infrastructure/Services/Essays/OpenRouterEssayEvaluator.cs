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

    public async Task<EssayEvaluationData> EvaluateAsync(string essayText, GradeLevel grade, string? topic, CancellationToken cancellationToken = default)
    {
        var messages = new[]
        {
            new ChatMessage { Role = "system", Content = EssayPrompts.GetSystem(grade, essayText, topic) },
            new ChatMessage { Role = "user", Content = essayText }
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

                return MapToResult(dto);
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

    private AiEssayDto ParseDto(string raw)
    {
        var json = ExtractJson(raw);
        var dto = JsonSerializer.Deserialize<AiEssayDto>(json, JsonOptions);
        return dto ?? throw new AiServiceException("AI cavabı boş qayıtdı.", isTransient: true);
    }

    private static EssayEvaluationData MapToResult(AiEssayDto dto)
    {
        if (string.Equals(dto.Status, "invalid", StringComparison.OrdinalIgnoreCase))
        {
            return new EssayEvaluationData
            {
                IsEssay = false,
                InvalidReason = dto.Reason ?? "The submitted text is not an essay."
            };
        }

        return new EssayEvaluationData
        {
            IsEssay = true,
            CorrectedEssay = dto.CorrectedEssay ?? string.Empty,
            Statistics = MapStatistics(dto.Statistics),
            Scores = MapScores(dto.Scores),
            Mistakes = MapMistakes(dto.Mistakes),
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

    private static EssayStatisticsDto MapStatistics(AiStatistics? s) =>
        s is null
            ? new EssayStatisticsDto(0, 0, 0, 0, 0)
            : new EssayStatisticsDto(s.Grammar, s.Spelling, s.Vocabulary, s.NaturalExpression, s.Total);

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
