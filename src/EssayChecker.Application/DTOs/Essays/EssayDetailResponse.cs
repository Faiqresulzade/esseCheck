using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Essays;

public sealed record EssayDetailResponse(
    int Id,
    string Title,
    DateTime CreatedAt,
    EssayInputSource Source,
    int WordCount,
    int AccuracyPercent,
    double TotalScore,
    string CorrectedEssay,
    EssayStatisticsDto Statistics,
    IReadOnlyList<EssayMistakeDto> Mistakes,
    EssayScoresDto Scores,
    TeacherFeedbackDto Feedback);
