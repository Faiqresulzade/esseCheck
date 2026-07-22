using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Auth;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>Uğurlu girişdə access + refresh token, uğursuzda səbəbini (yanlış məlumat/lockout) qaytarır.</summary>
    Task<LoginOutcome> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Refresh token-i yeni access + refresh token-ə dəyişir (rotasiya). Etibarsızdırsa null.</summary>
    Task<LoginResponse?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Çıxış — refresh token-i revoke edir.</summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<AuthResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<ProfileResponse?> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
}
