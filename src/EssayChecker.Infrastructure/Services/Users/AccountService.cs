using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Account;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace EssayChecker.Infrastructure.Services.Users;

public sealed class AccountService : IAccountService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISubscriptionRepository _subscriptions;

    public AccountService(
        UserManager<AppUser> userManager,
        IRefreshTokenRepository refreshTokens,
        ISubscriptionRepository subscriptions)
    {
        _userManager = userManager;
        _refreshTokens = refreshTokens;
        _subscriptions = subscriptions;
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

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return AuthResult.Failure(result.Errors.Select(e => e.Description).ToArray());

        // Sessiyaları və abunəlikləri bağla (soft delete — məlumat qalır).
        await _refreshTokens.RevokeAllAsync(userId, now, cancellationToken);
        await _subscriptions.DeactivateAllAsync(userId, now, cancellationToken);

        return AuthResult.Success("Hesab silindi.");
    }
}
