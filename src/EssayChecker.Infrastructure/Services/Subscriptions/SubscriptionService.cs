using System.Text;
using System.Text.Json;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Subscriptions;
using EssayChecker.Application.Settings;
using EssayChecker.Application.Subscriptions;
using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Domain.Entities.Users;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.GooglePlay;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Subscriptions;

public sealed class SubscriptionService : ISubscriptionService
{
    /// <summary>Referal mükafatı — dəvət edənin abunəliyinə əlavə olunan gün sayı (~aylıq dəyərin 20%-i).</summary>
    private const int ReferralBonusDays = 6;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISubscriptionRepository _repository;
    private readonly IGooglePlayPurchaseVerifier _googleVerifier;
    private readonly IProcessedNotificationRepository _processedNotifications;
    private readonly GooglePlaySettings _googlePlaySettings;
    private readonly TestingSettings _testingSettings;
    private readonly AppSettings _appSettings;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ISubscriptionRepository repository,
        IGooglePlayPurchaseVerifier googleVerifier,
        IProcessedNotificationRepository processedNotifications,
        IOptions<GooglePlaySettings> googlePlaySettings,
        IOptions<TestingSettings> testingSettings,
        IOptions<AppSettings> appSettings,
        UserManager<AppUser> userManager,
        ILogger<SubscriptionService> logger)
    {
        _repository = repository;
        _googleVerifier = googleVerifier;
        _processedNotifications = processedNotifications;
        _googlePlaySettings = googlePlaySettings.Value;
        _testingSettings = testingSettings.Value;
        _appSettings = appSettings.Value;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<SubscriptionPlan> GetActivePlanAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Closed testing: Google Play-in test alışlarını sürətləndirilmiş dövrədə avtomatik
        // ləğv etməsi testçiləri narahat etdiyi üçün, bu bayraq aktivdirsə real abunəlik
        // vəziyyətindən asılı olmayaraq hamıya Pro Plus hüquqları verilir.
        if (_testingSettings.ForceProPlusForAllUsers)
            return SubscriptionPlan.ProPlus;

        var subscription = await _repository.GetActiveAsync(userId, cancellationToken);

        if (subscription is null)
            return SubscriptionPlan.Free;

        if (IsExpired(subscription))
        {
            // Abunəlik bitib — avtomatik Free-yə düşür.
            await _repository.DeactivateAllAsync(userId, DateTime.UtcNow, cancellationToken);
            return SubscriptionPlan.Free;
        }

        return subscription.Plan;
    }

    public async Task<SubscriptionResponse> GetMySubscriptionAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (_testingSettings.ForceProPlusForAllUsers)
            return TestingProPlusResponse();

        var subscription = await _repository.GetActiveAsync(userId, cancellationToken);

        if (subscription is null || IsExpired(subscription))
        {
            if (subscription is not null)
                await _repository.DeactivateAllAsync(userId, DateTime.UtcNow, cancellationToken);

            return FreeResponse();
        }

        return Map(subscription);
    }

    public async Task<SubscriptionResponse> SubscribeAsync(int userId, SubscribeRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Əvvəlki aktiv abunəlikləri deaktiv et.
        await _repository.DeactivateAllAsync(userId, now, cancellationToken);

        var subscription = new UserSubscription
        {
            UserId = userId,
            Plan = request.Plan,
            StartDate = now,
            EndDate = now.AddDays(request.DurationDays),
            IsActive = true,
            AutoRenew = request.AutoRenew,
            PurchaseToken = request.PurchaseToken,
            Platform = request.Platform,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.AddAsync(subscription, cancellationToken);
        return Map(subscription);
    }

    public async Task<SubscriptionResponse> CancelAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _repository.DeactivateAllAsync(userId, DateTime.UtcNow, cancellationToken);
        return FreeResponse();
    }

    public IReadOnlyList<PlanInfoResponse> GetPlans() => PlanCatalog.All;

    public async Task<SubscriptionResponse> VerifyGooglePurchaseAsync(
        int userId,
        VerifyGooglePurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_googlePlaySettings.Products.TryGetValue(request.ProductId, out var plan))
            throw new InvalidOperationException($"Naməlum Google Play məhsulu: {request.ProductId}");

        var state = await _googleVerifier.VerifyAsync(request.ProductId, request.PurchaseToken, cancellationToken);
        if (!state.IsActive)
            throw new InvalidOperationException("Satınalma aktiv deyil (bitib və ya ləğv edilib).");

        // Eyni purchaseToken başqa istifadəçiyə aiddirsə rədd et (fraud/paylaşılan token qarşısı).
        var existing = await _repository.GetByPurchaseTokenAsync(request.PurchaseToken, cancellationToken);
        if (existing is not null && existing.UserId != userId)
            throw new InvalidOperationException("Bu satınalma artıq başqa hesaba bağlıdır.");

        if (!state.IsAcknowledged)
            await _googleVerifier.AcknowledgeAsync(request.ProductId, request.PurchaseToken, cancellationToken);

        // Referal mükafatı yalnız istifadəçinin HƏQİQƏTƏN İLK abunəliyində verilməlidir (yoxsa
        // hər yeniləmə/plan dəyişikliyi təkrar mükafat gətirərdi) — ona görə bu, hər hansı
        // mutasiyadan (DeactivateAllAsync, Add/Update) ƏVVƏL, təmiz vəziyyətdə yoxlanılır.
        // Proqram deaktivdirsə (AppSettings.ReferralProgramEnabled=false) lazımsız sorğu edilmir.
        var isFirstEverSubscription = _appSettings.ReferralProgramEnabled
            && !await _repository.HasAnyAsync(userId, cancellationToken);

        var now = DateTime.UtcNow;

        // Bu istifadəçinin əvvəlki aktiv abunəliklərini (fərqli plan/token) deaktiv et.
        await _repository.DeactivateAllAsync(userId, now, cancellationToken);

        UserSubscription subscription;
        if (existing is not null)
        {
            existing.Plan = plan;
            existing.EndDate = state.ExpiryUtc;
            existing.AutoRenew = state.AutoRenewing;
            existing.ProductId = request.ProductId;
            existing.IsActive = true;
            existing.UpdatedAt = now;
            await _repository.UpdateAsync(existing, cancellationToken);
            subscription = existing;
        }
        else
        {
            subscription = new UserSubscription
            {
                UserId = userId,
                Plan = plan,
                StartDate = now,
                EndDate = state.ExpiryUtc,
                IsActive = true,
                AutoRenew = state.AutoRenewing,
                PurchaseToken = request.PurchaseToken,
                PurchaseTokenHash = PurchaseTokenHasher.Hash(request.PurchaseToken),
                ProductId = request.ProductId,
                Platform = SubscriptionPlatform.GooglePlay,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _repository.AddAsync(subscription, cancellationToken);
        }

        if (isFirstEverSubscription)
            await TryGrantReferralRewardAsync(userId, cancellationToken);

        return Map(subscription);
    }

    /// <summary>
    /// <paramref name="referredUserId"/> kiminsə referal koduyla qeydiyyatdan keçibsə və mükafat
    /// hələ verilməyibsə, dəvət edənin abunəliyinə <see cref="ReferralBonusDays"/> gün əlavə edir.
    /// Bu, gözlənilməz vəziyyətlərdə (referrer tapılmır və s.) əsas satınalma axınını pozmamalıdır
    /// — ona görə bütün xətalar udulub yalnız loglanır.
    /// </summary>
    private async Task TryGrantReferralRewardAsync(int referredUserId, CancellationToken cancellationToken)
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

    public async Task ProcessGooglePlayNotificationAsync(
        string base64Data,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        // Pub/Sub "at-least-once" çatdırır — eyni mesaj təkrar gələ bilər.
        if (!await _processedNotifications.TryMarkProcessedAsync(messageId, cancellationToken))
            return;

        GoogleRtdnEnvelope? envelope;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Data));
            envelope = JsonSerializer.Deserialize<GoogleRtdnEnvelope>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return;
        }

        var notification = envelope?.SubscriptionNotification;
        if (notification is null
            || string.IsNullOrWhiteSpace(notification.PurchaseToken)
            || string.IsNullOrWhiteSpace(notification.SubscriptionId))
        {
            return; // test bildirişi ("Send test notification") və ya aidiyyəti olmayan payload
        }

        var existing = await _repository.GetByPurchaseTokenAsync(notification.PurchaseToken, cancellationToken);
        if (existing is null)
            return; // bizim tanımadığımız token

        var now = DateTime.UtcNow;

        if (notification.NotificationType == GoogleRtdnNotificationType.Revoked)
        {
            existing.IsActive = false;
            existing.UpdatedAt = now;
            await _repository.UpdateAsync(existing, cancellationToken);
            return;
        }

        // Bildirişin növünə baxmayaraq, həqiqət mənbəyi kimi Google-dan yenidən doğrulayırıq.
        var state = await _googleVerifier.VerifyAsync(notification.SubscriptionId, notification.PurchaseToken, cancellationToken);

        if (state.IsActive)
            await _repository.DeactivateOthersAsync(existing.UserId, existing.Id, now, cancellationToken);

        existing.EndDate = state.ExpiryUtc;
        existing.AutoRenew = state.AutoRenewing;
        existing.IsActive = state.IsActive;
        existing.UpdatedAt = now;
        await _repository.UpdateAsync(existing, cancellationToken);
    }

    private static bool IsExpired(UserSubscription s) =>
        s.EndDate.HasValue && s.EndDate.Value <= DateTime.UtcNow;

    private static SubscriptionResponse FreeResponse() =>
        new(SubscriptionPlan.Free, true, null, null, false, null);

    /// <summary>Closed testing zamanı UI-da göstərilən "sahə" statusu — real satınalma deyil.</summary>
    private static SubscriptionResponse TestingProPlusResponse() =>
        new(SubscriptionPlan.ProPlus, true, null, null, false, null);

    private static SubscriptionResponse Map(UserSubscription s) =>
        new(s.Plan, s.IsActive, s.StartDate, s.EndDate, s.AutoRenew, s.Platform);
}
