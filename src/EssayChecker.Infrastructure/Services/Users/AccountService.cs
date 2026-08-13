using System.Security.Cryptography;
using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Account;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using EssayChecker.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Users;

public sealed class AccountService : IAccountService
{
    // Qarışıq düşə bilən simvollar (0/O, 1/I) qəsdən çıxarılıb ki, istifadəçi kodu əl ilə
    // yazanda səhv etməsin.
    private const string ReferralCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int ReferralCodeLength = 8;
    private const int ReferralCodeMaxGenerationAttempts = 5;

    private readonly UserManager<AppUser> _userManager;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly AppSettings _appSettings;

    public AccountService(
        UserManager<AppUser> userManager,
        IRefreshTokenRepository refreshTokens,
        ISubscriptionRepository subscriptions,
        IOptions<AppSettings> appSettings)
    {
        _userManager = userManager;
        _refreshTokens = refreshTokens;
        _subscriptions = subscriptions;
        _appSettings = appSettings.Value;
    }

    public async Task<AuthResult> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return AuthResult.Failure("İstifadəçi tapılmadı.");

        user.FullName = request.FullName;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? AuthResult.Success("Profil yeniləndi.")
            : AuthResult.Failure(result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<AuthResult> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return AuthResult.Failure("İstifadəçi tapılmadı.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return AuthResult.Failure(result.Errors.Select(e => e.Description).ToArray());

        // Şifrə dəyişdi — bütün aktiv sessiyaları bağla.
        await _refreshTokens.RevokeAllAsync(userId, DateTime.UtcNow, cancellationToken);

        return AuthResult.Success("Şifrə uğurla dəyişdirildi.");
    }

    public async Task<AuthResult> DeleteAccountAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsDeleted)
            return AuthResult.Failure("İstifadəçi tapılmadı.");

        var now = DateTime.UtcNow;
        user.IsDeleted = true;
        user.DeletedAt = now;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return AuthResult.Failure(result.Errors.Select(e => e.Description).ToArray());

        // Sessiyaları və abunəlikləri bağla (soft delete — 30 gün sonra AccountPurgeService
        // tərəfindən bərpaolunmaz silinəcək, bax /legal/delete-account səhifəsi).
        await _refreshTokens.RevokeAllAsync(userId, now, cancellationToken);
        await _subscriptions.DeactivateAllAsync(userId, now, cancellationToken);

        return AuthResult.Success("Hesab silindi.");
    }

    public async Task<ReferralInfoResponse> GetReferralInfoAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Proqram hələ rəsmən aktiv deyil (bax AppSettings.ReferralProgramEnabled) — bu halda
        // boş-yerə kod yaratmırıq, sadəcə "hələ mövcud deyil" bildiririk.
        if (!_appSettings.ReferralProgramEnabled)
            return new ReferralInfoResponse(false, null, null);

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("İstifadəçi tapılmadı.");

        if (string.IsNullOrEmpty(user.ReferralCode))
        {
            user.ReferralCode = await GenerateUniqueReferralCodeAsync(cancellationToken);
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException("Referal kodu yaradıla bilmədi.");
        }

        return new ReferralInfoResponse(true, user.ReferralCode, _appSettings.PlayStoreUrl);
    }

    /// <summary>
    /// Toqquşma ehtimalı 8 simvollu əlifbadan 33^8 ≈ 1.1 trilyon variantla praktik olaraq
    /// sıfıra yaxındır, amma qəsdən sonsuz loopa düşməsin deyə cəhd sayı məhduddur (bax
    /// layihədəki digər "bounded retry" naxışları — AI fallback, essay-evaluator).
    /// </summary>
    private async Task<string> GenerateUniqueReferralCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < ReferralCodeMaxGenerationAttempts; attempt++)
        {
            var candidate = GenerateRandomCode();
            var taken = await _userManager.Users
                .AnyAsync(u => u.ReferralCode == candidate, cancellationToken);

            if (!taken)
                return candidate;
        }

        throw new InvalidOperationException(
            $"Unikal referal kodu {ReferralCodeMaxGenerationAttempts} cəhddən sonra yaradıla bilmədi.");
    }

    private static string GenerateRandomCode()
    {
        Span<char> chars = stackalloc char[ReferralCodeLength];
        for (var i = 0; i < ReferralCodeLength; i++)
            chars[i] = ReferralCodeAlphabet[RandomNumberGenerator.GetInt32(ReferralCodeAlphabet.Length)];

        return new string(chars);
    }
}
