namespace EssayChecker.Application.DTOs.Essays;

public sealed record EssayHistoryResponse(
    IReadOnlyList<EssayListItemResponse> Items,
    int TotalCount,
    double AverageScore,
    int Page,
    int PageSize,
    int TotalPages);
