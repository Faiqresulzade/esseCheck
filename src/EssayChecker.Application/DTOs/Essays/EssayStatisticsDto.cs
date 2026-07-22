namespace EssayChecker.Application.DTOs.Essays;

public sealed record EssayStatisticsDto(
    int Grammar,
    int Spelling,
    int Vocabulary,
    int NaturalExpression,
    int Total);
