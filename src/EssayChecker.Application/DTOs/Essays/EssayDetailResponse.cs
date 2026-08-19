using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Essays;

public sealed record EssayDetailResponse(
    int Id,
    string Title,
    DateTime CreatedAt,
    EssayInputSource Source,
    GradeLevel Grade,
    int WordCount,
    int AccuracyPercent,
    double TotalScore,
    string CorrectedEssay,
    EssayStatisticsDto Statistics,
    IReadOnlyList<EssayMistakeDto> Mistakes,
    EssayScoresDto Scores,
    TeacherFeedbackDto Feedback,
    int? StudentId = null,
    string? StudentName = null);
