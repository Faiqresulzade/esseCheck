using System.Security.Cryptography;
using System.Text;
using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Auth;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using EssayChecker.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Users;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly AppSettings _appSettings;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<AppUser> userManager,
        IJwtService jwtService,
        IEmailService emailService,
        IRefreshTokenRepository refreshTokens,
        IOptions<AppSettings> appSettings,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _emailService = emailService;
        _refreshTokens = refreshTokens;
        _appSettings = appSettings.Value;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.AcceptTerms)
            return AuthResult.Failure("İstifadə şərtləri və Gizlilik siyasəti qəbul edilməlidir.");

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return AuthResult.Failure("Bu e-mail ünvanı artıq qeydiyyatdan keçib.");

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return AuthResult.Failure(result.Errors.Select(e => e.Description).ToArray());

        return AuthResult.Success("Qeydiyyat uğurla tamamlandı.");
    }

    public async Task<LoginOutcome> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || user.IsDeleted)
            return LoginOutcome.Invalid();

        if (await _userManager.IsLockedOutAsync(user))
            return LoginOutcome.LockedOut(await _userManager.GetLockoutEndDateAsync(user));

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);

            // Məhz bu cəhd limiti doldurub lockout-u işə salıbsa, dərhal bunu bildirək.
            if (await _userManager.IsLockedOutAsync(user))
                return LoginOutcome.LockedOut(await _userManager.GetLockoutEndDateAsync(user));

            return LoginOutcome.Invalid();
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var now = DateTime.UtcNow;
        user.LastLoginDate = now;
        await _userManager.UpdateAsync(user);

        var response = await IssueTokensAsync(user, now, cancellationToken);
        return LoginOutcome.Success(response);
    }

    public async Task<LoginResponse?> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var now = DateTime.UtcNow;
        var stored = await _refreshTokens.GetByHashAsync(Hash(refreshToken), cancellationToken);
        if (stored is null || !stored.IsActive(now))
            return null;

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || user.IsDeleted)
            return null;

        // Rotasiya: yeni token yarat, köhnəni revoke et.
        var response = await IssueTokensAsync(user, now, cancellationToken);
        stored.RevokedAt = now;
        stored.ReplacedByTokenHash = Hash(response!.RefreshToken);
        await _refreshTokens.UpdateAsync(stored, cancellationToken);

        return response;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var stored = await _refreshTokens.GetByHashAsync(Hash(refreshToken), cancellationToken);
        if (stored is null || stored.RevokedAt is not null)
            return;

        stored.RevokedAt = DateTime.UtcNow;
        await _refreshTokens.UpdateAsync(stored, cancellationToken);
    }

    public async Task<AuthResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        // Təhlükəsizlik: e-mail-in mövcud olub-olmadığını bildirmirik.
        if (user is not null && !user.IsDeleted)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink =
                $"{_appSettings.ResetPasswordUrl}?email={Uri.EscapeDataString(user.Email!)}" +
                $"&token={Uri.EscapeDataString(token)}";

            await _emailService.SendAsync(
                user.Email!,
                "Şifrə sıfırlama",
                BuildResetPasswordEmail(user.FullName, resetLink),
                cancellationToken);
        }

        return AuthResult.Success(
            "Əgər e-mail ünvanı sistemdə mövcuddursa, şifrə sıfırlama linki göndərildi.");
    }

    public async Task<AuthResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || user.IsDeleted)
            return AuthResult.Failure("Şifrə sıfırlama uğursuz oldu.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return AuthResult.Failure(result.Errors.Select(e => e.Description).ToArray());

        // Şifrə dəyişdi — bütün refresh token-ləri etibarsız et.
        await _refreshTokens.RevokeAllAsync(user.Id, DateTime.UtcNow, cancellationToken);

        await _emailService.SendAsync(
            user.Email!,
            "Şifrəniz dəyişdirildi",
            BuildPasswordChangedEmail(user.FullName),
            cancellationToken);

        return AuthResult.Success("Şifrəniz uğurla dəyişdirildi.");
    }

    public async Task<ProfileResponse?> GetProfileAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return null;

        return new ProfileResponse(
            user.Id,
            user.FullName,
            user.Email!,
            user.CreatedAt,
            user.LastLoginDate);
    }

    private async Task<LoginResponse> IssueTokensAsync(AppUser user, DateTime now, CancellationToken cancellationToken)
    {
        var (token, expiresAt) = _jwtService.GenerateToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, now, cancellationToken);
        return new LoginResponse(token, refreshToken, expiresAt, user.FullName, user.Email!);
    }

    private async Task<string> CreateRefreshTokenAsync(int userId, DateTime now, CancellationToken cancellationToken)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(raw),
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenDays),
            CreatedAt = now
        }, cancellationToken);

        return raw;
    }

    private static string Hash(string value) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string BuildResetPasswordEmail(string fullName, string resetLink) => $"""
        <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto">
          <h2 style="color:#2563eb">EssayCheck AI</h2>
          <p>Salam, {fullName}!</p>
          <p>Şifrənizi sıfırlamaq üçün aşağıdakı düyməyə klikləyin:</p>
          <p style="text-align:center;margin:28px 0">
            <a href="{resetLink}"
               style="background:#2563eb;color:#fff;padding:12px 28px;border-radius:8px;
                      text-decoration:none;display:inline-block">Şifrəni sıfırla</a>
          </p>
          <p style="color:#6b7280;font-size:13px">
            Bu sorğunu siz göndərməmisinizsə, bu e-maili nəzərə almayın.
          </p>
        </div>
        """;

    private static string BuildPasswordChangedEmail(string fullName) => $"""
        <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto">
          <h2 style="color:#2563eb">EssayCheck AI</h2>
          <p>Salam, {fullName}!</p>
          <p style="color:#16a34a;font-weight:bold">Şifrəniz uğurla dəyişdirildi.</p>
          <p style="color:#6b7280;font-size:13px">
            Bu əməliyyatı siz etməmisinizsə, dərhal bizimlə əlaqə saxlayın.
          </p>
        </div>
        """;
}
