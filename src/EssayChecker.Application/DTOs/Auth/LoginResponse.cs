namespace EssayChecker.Application.DTOs.Auth;

public sealed record LoginResponse(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    string FullName,
    string Email);
