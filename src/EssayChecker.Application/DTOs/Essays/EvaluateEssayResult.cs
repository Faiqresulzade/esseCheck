namespace EssayChecker.Application.DTOs.Essays;

/// <summary>
/// Qiymətləndirmə nəticəsi. AI mətni esse saymadıqda Success=false və Error doldurulur.
/// </summary>
public sealed record EvaluateEssayResult(bool Success, string? Error, EssayDetailResponse? Essay);
