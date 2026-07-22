namespace EssayChecker.Application.DTOs.Essays;

public sealed record TeacherFeedbackDto(
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> Recommendations);
