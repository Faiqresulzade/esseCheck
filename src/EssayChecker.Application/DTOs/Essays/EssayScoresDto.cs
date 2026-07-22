namespace EssayChecker.Application.DTOs.Essays;

public sealed record EssayScoresDto(
    double Structure,
    double Content,
    double Grammar,
    double Vocabulary,
    double Total);
