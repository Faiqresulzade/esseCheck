using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Essays;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.Ai;

namespace EssayChecker.Infrastructure.Services.Essays;

public sealed class EssayService : IEssayService
{
    private readonly IEssayEvaluator _evaluator;
    private readonly IOcrService _ocrService;
    private readonly IEssayRepository _repository;
    private readonly ITeachingRepository _teachingRepository;

    public EssayService(
        IEssayEvaluator evaluator,
        IOcrService ocrService,
        IEssayRepository repository,
        ITeachingRepository teachingRepository)
    {
        _evaluator = evaluator;
        _ocrService = ocrService;
        _repository = repository;
        _teachingRepository = teachingRepository;
    }

    /// <param name="grade">
    /// Controller tərəfindən artıq həll edilmiş sinif: sorğudakı dəyər, o yoxdursa şagird
    /// kartındakı dəyər. Hər ikisi boş olduqda controller sorğunu buraxmır.
    /// </param>
    public async Task<EvaluateEssayResult> EvaluateAsync(
        int userId,
        EvaluateEssayRequest request,
        GradeLevel grade,
        CancellationToken cancellationToken = default)
    {
        var data = await _evaluator.EvaluateAsync(
            request.Text, grade, request.Topic, promptImages: null, cancellationToken);

        return await PersistAndMapAsync(
            userId, data, request.Text, request.Title, request.StudentId, grade, request.Source, cancellationToken);
    }

    /// <summary>
    /// 9-cu sinif — DİM formatı: tələbə 3 promt-şəklini (yazı tapşırığının əsaslandığı şəkillər)
    /// göndərir, AI essenin content balını bu şəkillərdə göstərilənlərə görə qiymətləndirir.
    /// Mövzu (topic) sahəsi mənasızdır, ona görə istifadə olunmur.
    /// </summary>
    public async Task<EvaluateEssayResult> EvaluateGrade9WithImagesAsync(
        int userId,
        string text,
        string? title,
        int? studentId,
        IReadOnlyList<PromptImage> promptImages,
        CancellationToken cancellationToken = default)
    {
        var data = await _evaluator.EvaluateAsync(
            text, GradeLevel.Grade9, topic: null, promptImages, cancellationToken);

        return await PersistAndMapAsync(
            userId, data, text, title, studentId, GradeLevel.Grade9, EssayInputSource.Text, cancellationToken);
    }

    private async Task<EvaluateEssayResult> PersistAndMapAsync(
        int userId,
        EssayEvaluationData data,
        string originalText,
        string? title,
        int? studentId,
        GradeLevel grade,
        EssayInputSource source,
        CancellationToken cancellationToken)
    {
        if (!data.IsEssay)
            return new EvaluateEssayResult(false, data.InvalidReason, null);

        var essay = new Essay
        {
            UserId = userId,
            StudentId = studentId,
            Title = ResolveTitle(title, originalText),
            OriginalText = originalText,
            CorrectedEssay = data.CorrectedEssay,
            WordCount = EssayPrompts.CountWords(originalText),
            TotalScore = data.Scores.Total,
            AccuracyPercent = (int)Math.Round(data.Scores.Total / 5.0 * 100),
            InputSource = source,
            Grade = grade,
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
                StructureComment = data.Scores.StructureComment,
                Content = data.Scores.Content,
                ContentComment = data.Scores.ContentComment,
                Grammar = data.Scores.Grammar,
                GrammarComment = data.Scores.GrammarComment,
                Vocabulary = data.Scores.Vocabulary,
                VocabularyComment = data.Scores.VocabularyComment,
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

        return new EvaluateEssayResult(true, null, await MapToDetailAsync(essay, cancellationToken));
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
        int? studentId,
        int? groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _repository.GetHistoryAsync(userId, search, studentId, groupId, page, pageSize, cancellationToken);

    public async Task<EssayDetailResponse?> GetByIdAsync(
        int userId,
        int essayId,
        CancellationToken cancellationToken = default)
    {
        var essay = await _repository.GetByIdAsync(userId, essayId, cancellationToken);
        return essay is null ? null : await MapToDetailAsync(essay, cancellationToken);
    }

    public Task<bool> DeleteAsync(
        int userId,
        int essayId,
        CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(userId, essayId, cancellationToken);

    public Task<int> DeleteAllAsync(int userId, CancellationToken cancellationToken = default) =>
        _repository.DeleteAllAsync(userId, cancellationToken);

    /// <summary>
    /// Şagird adı ayrıca oxunur (silinmiş şagird üçün də) — esse detalında "kimin essesi"
    /// göstərilməlidir. Şagird bağlantısı yoxdursa əlavə sorğu getmir.
    /// </summary>
    private async Task<EssayDetailResponse> MapToDetailAsync(Essay e, CancellationToken cancellationToken)
    {
        var studentName = e.StudentId is null
            ? null
            : await _teachingRepository.GetStudentNameAsync(e.StudentId.Value, cancellationToken);

        return MapToDetail(e, studentName);
    }

    private static EssayDetailResponse MapToDetail(Essay e, string? studentName) => new(
        e.Id,
        e.Title,
        e.CreatedAt,
        e.InputSource,
        e.Grade,
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
            e.Scores.StructureComment,
            e.Scores.Content,
            e.Scores.ContentComment,
            e.Scores.Grammar,
            e.Scores.GrammarComment,
            e.Scores.Vocabulary,
            e.Scores.VocabularyComment,
            e.Scores.Total),
        new TeacherFeedbackDto(
            e.Feedback.Strengths,
            e.Feedback.Weaknesses,
            e.Feedback.Recommendations),
        e.StudentId,
        studentName);

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
