namespace EssayChecker.Application.DTOs.Auth;

public sealed record ProfileResponse(
    int Id,
    string FullName,
    string Email,
    DateTime CreatedAt,
    DateTime? LastLoginDate);
