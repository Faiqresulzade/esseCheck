using System.Text.Json;
using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.Ai;

namespace EssayChecker.Infrastructure.Services.Essays;

internal sealed class OpenRouterEssayEvaluator : IEssayEvaluator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OpenRouterClient _client;
    private readonly Application.Settings.OpenRouterSettings _settings;

    public OpenRouterEssayEvaluator(
        OpenRouterClient client,
        Microsoft.Extensions.Options.IOptions<Application.Settings.OpenRouterSettings> options)
    {
        _client = client;
        _settings = options.Value;
    }

    public async Task<EssayEvaluationData> EvaluateAsync(string essayText, CancellationToken cancellationToken = default)
    {
        var messages = new[]
        {
            new ChatMessage { Role = "system", Content = EssayPrompts.System },
            new ChatMessage { Role = "user", Content = essayText }
        };

        var raw = await _client.CompleteAsync(_settings.Model, messages, cancellationToken);
        var json = ExtractJson(raw);

        AiEssayDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<AiEssayDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new AiServiceException($"AI cavabı düzgün JSON deyil: {ex.Message}", innerException: ex);
        }

        if (dto is null)
            throw new AiServiceException("AI cavabı boş qayıtdı.");

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
            ? new EssayScoresDto(0, 0, 0, 0, 0)
            : new EssayScoresDto(s.Structure, s.Content, s.Grammar, s.Vocabulary, s.Total);

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
        public double Content { get; set; }
        public double Grammar { get; set; }
        public double Vocabulary { get; set; }
        public double Total { get; set; }
    }

    private sealed class AiFeedback
    {
        public List<string>? Strengths { get; set; }
        public List<string>? Weaknesses { get; set; }
        public List<string>? Recommendations { get; set; }
    }
}
