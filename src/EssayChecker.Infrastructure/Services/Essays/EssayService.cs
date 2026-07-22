using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Essays;

namespace EssayChecker.Infrastructure.Services.Essays;

public sealed class EssayService : IEssayService
{
    private readonly IEssayEvaluator _evaluator;
    private readonly IOcrService _ocrService;
    private readonly IEssayRepository _repository;

    public EssayService(
        IEssayEvaluator evaluator,
        IOcrService ocrService,
        IEssayRepository repository)
    {
        _evaluator = evaluator;
        _ocrService = ocrService;
        _repository = repository;
    }

    public async Task<EvaluateEssayResult> EvaluateAsync(
        int userId,
        EvaluateEssayRequest request,
        CancellationToken cancellationToken = default)
    {
        var data = await _evaluator.EvaluateAsync(request.Text, cancellationToken);

        if (!data.IsEssay)
            return new EvaluateEssayResult(false, data.InvalidReason, null);

        var essay = new Essay
        {
            UserId = userId,
            Title = ResolveTitle(request.Title, request.Text),
            OriginalText = request.Text,
            CorrectedEssay = data.CorrectedEssay,
            WordCount = CountWords(request.Text),
            TotalScore = data.Scores.Total,
            AccuracyPercent = (int)Math.Round(data.Scores.Total / 5.0 * 100),
            InputSource = request.Source,
            CreatedAt = DateTime.UtcNow,
            Statistics = new EssayStatistics
            {
                Grammar = data.Statistics.Grammar,
                Spelling = data.Statistics.Spelling,
                Vocabulary = data.Statistics.Vocabulary,
                NaturalExpression = data.Statistics.NaturalExpression,
                Total = data.Statistics.Total
            },
            Scores = new EssayScores
            {
                Structure = data.Scores.Structure,
                Content = data.Scores.Content,
                Grammar = data.Scores.Grammar,
                Vocabulary = data.Scores.Vocabulary,
                Total = data.Scores.Total
            },
            Feedback = new TeacherFeedback
            {
                Strengths = data.Feedback.Strengths.ToList(),
                Weaknesses = data.Feedback.Weaknesses.ToList(),
                Recommendations = data.Feedback.Recommendations.ToList()
            },
            Mistakes = data.Mistakes
                .Select(m => new EssayMistake
                {
                    Wrong = m.Wrong,
                    Correct = m.Correct,
                    Category = m.Category,
                    Reason = m.Reason
                })
                .ToList()
        };

        await _repository.AddAsync(essay, cancellationToken);

        return new EvaluateEssayResult(true, null, MapToDetail(essay));
    }

    public async Task<OcrResponse> ReadImageAsync(
        Stream imageStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var text = await _ocrService.ExtractTextAsync(imageStream, contentType, cancellationToken);
        return new OcrResponse(text);
    }

    public Task<EssayHistoryResponse> GetHistoryAsync(
        int userId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _repository.GetHistoryAsync(userId, search, page, pageSize, cancellationToken);

    public async Task<EssayDetailResponse?> GetByIdAsync(
        int userId,
        int essayId,
        CancellationToken cancellationToken = default)
    {
        var essay = await _repository.GetByIdAsync(userId, essayId, cancellationToken);
        return essay is null ? null : MapToDetail(essay);
    }

    public Task<bool> DeleteAsync(
        int userId,
        int essayId,
        CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(userId, essayId, cancellationToken);

    public Task<int> DeleteAllAsync(int userId, CancellationToken cancellationToken = default) =>
        _repository.DeleteAllAsync(userId, cancellationToken);

    private static EssayDetailResponse MapToDetail(Essay e) => new(
        e.Id,
        e.Title,
        e.CreatedAt,
        e.InputSource,
        e.WordCount,
        e.AccuracyPercent,
        e.TotalScore,
        e.CorrectedEssay,
        new EssayStatisticsDto(
            e.Statistics.Grammar,
            e.Statistics.Spelling,
            e.Statistics.Vocabulary,
            e.Statistics.NaturalExpression,
            e.Statistics.Total),
        e.Mistakes
            .Select(m => new EssayMistakeDto(m.Wrong, m.Correct, m.Category, m.Reason))
            .ToList(),
        new EssayScoresDto(
            e.Scores.Structure,
            e.Scores.Content,
            e.Scores.Grammar,
            e.Scores.Vocabulary,
            e.Scores.Total),
        new TeacherFeedbackDto(
            e.Feedback.Strengths,
            e.Feedback.Weaknesses,
            e.Feedback.Recommendations));

    private static int CountWords(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static string ResolveTitle(string? title, string text)
    {
        if (!string.IsNullOrWhiteSpace(title))
            return title.Trim();

        var firstLine = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstLine))
            return "Esse";

        return firstLine.Length <= 60 ? firstLine : firstLine[..60].TrimEnd() + "…";
    }
}
