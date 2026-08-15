using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Domain.Entities.Users;
using EssayChecker.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Subscriptions;

public sealed class ReferralRewardService : IReferralRewardService
{
    /// <summary>Referal mükafatı — dəvət edənin abunəliyinə əlavə olunan gün sayı (~aylıq dəyərin 20%-i).</summary>
    private const int ReferralBonusDays = 6;

    private readonly ISubscriptionRepository _repository;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppSettings _appSettings;
    private readonly ILogger<ReferralRewardService> _logger;

    public ReferralRewardService(
        ISubscriptionRepository repository,
        UserManager<AppUser> userManager,
        IOptions<AppSettings> appSettings,
        ILogger<ReferralRewardService> logger)
    {
        _repository = repository;
        _userManager = userManager;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    public async Task<bool> IsRewardTriggerAsync(int userId, CancellationToken cancellationToken = default) =>
        // Proqram deaktivdirsə (AppSettings.ReferralProgramEnabled=false) lazımsız sorğu edilmir.
        _appSettings.ReferralProgramEnabled
        && !await _repository.HasAnyAsync(userId, cancellationToken);

    public async Task TryGrantRewardAsync(int referredUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_appSettings.ReferralProgramEnabled)
                return;

            var referredUser = await _userManager.FindByIdAsync(referredUserId.ToString());
            if (referredUser?.ReferredByUserId is not int referrerId || referredUser.ReferralRewardGranted)
                return;

            var now = DateTime.UtcNow;
            var referrerSubscription = await _repository.GetMostRecentAsync(referrerId, cancellationToken);

            // Mükafat həmişə TAM 6 real gün verməlidir — abunəlik artıq bitmişsə "indi"dən,
            // hələ aktivdirsə mövcud bitmə tarixindən hesablanır (əks halda köhnə tarixə 6 gün
            // əlavə etmək istifadəçiyə faktiki heç nə qazandırmazdı).
            var baseDate = referrerSubscription?.EndDate is { } existingEnd && existingEnd > now ? existingEnd : now;
            var newEndDate = baseDate.AddDays(ReferralBonusDays);

            if (referrerSubscription is null)
            {
                // Gözlənilməz hal — dəvət linkini paylaşmaq üçün istifadəçi artıq abunə olmalı
                // idi. Bonusu itirməmək üçün yenə də yeni bir abunəlik qeydi yaradılır.
                referrerSubscription = new UserSubscription
                {
                    UserId = referrerId,
                    Plan = SubscriptionPlan.Pro,
                    StartDate = now,
                    EndDate = newEndDate,
                    IsActive = true,
                    Platform = SubscriptionPlatform.Manual,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _repository.AddAsync(referrerSubscription, cancellationToken);
            }
            else
            {
                referrerSubscription.EndDate = newEndDate;
                referrerSubscription.IsActive = true;
                referrerSubscription.UpdatedAt = now;
                await _repository.UpdateAsync(referrerSubscription, cancellationToken);
            }

            referredUser.ReferralRewardGranted = true;
            await _userManager.UpdateAsync(referredUser);

            _logger.LogInformation(
                "Referal mükafatı verildi: dəvət edən {ReferrerId} istifadəçisinin abunəliyi {NewEndDate}-ə qədər uzadıldı (dəvət edilən: {ReferredUserId}).",
                referrerId, newEndDate, referredUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Referal mükafatı verilərkən xəta baş verdi (dəvət edilən istifadəçi: {ReferredUserId}). Əsas satınalma axını təsirlənmədi.",
                referredUserId);
        }
    }
}
