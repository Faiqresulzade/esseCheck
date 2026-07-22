namespace EssayChecker.Application.DTOs.Essays;

public sealed record EssayListItemResponse(
    int Id,
    string Title,
    DateTime CreatedAt,
    int WordCount,
    double TotalScore);
