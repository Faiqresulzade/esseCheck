namespace EssayChecker.Application.DTOs.Essays;

/// <summary>
/// AI-dan parse edilmiş xam qiymətləndirmə (persistensiyadan əvvəl).
/// <see cref="IsEssay"/> false olduqda mətn esse deyil.
/// </summary>
public sealed class EssayEvaluationData
{
    public bool IsEssay { get; init; } = true;

    public string? InvalidReason { get; init; }

    public string CorrectedEssay { get; init; } = string.Empty;

    public EssayStatisticsDto Statistics { get; init; } = new(0, 0, 0, 0, 0);

    public IReadOnlyList<EssayMistakeDto> Mistakes { get; init; } = Array.Empty<EssayMistakeDto>();

    public EssayScoresDto Scores { get; init; } = new(0, 0, 0, 0, 0);

    public TeacherFeedbackDto Feedback { get; init; } =
        new(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
}
