using EssayChecker.Application.DTOs.Subscriptions;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface ISubscriptionService
{
    /// <summary>
    /// İstifadəçinin aktiv planını qaytarır. Abunəlik bitibsə avtomatik Free-yə düşür
    /// (və bazada deaktiv edilir).
    /// </summary>
    Task<SubscriptionPlan> GetActivePlanAsync(int userId, CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> GetMySubscriptionAsync(int userId, CancellationToken cancellationToken = default);

    Task<SubscriptionResponse> SubscribeAsync(int userId, SubscribeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Abunəliyi ləğv edir (Free-yə keçir).</summary>
    Task<SubscriptionResponse> CancelAsync(int userId, CancellationToken cancellationToken = default);

    IReadOnlyList<PlanInfoResponse> GetPlans();

    /// <summary>
    /// Client Google Play Billing ilə satınalma etdikdən sonra çağırır. PurchaseToken birbaşa
    /// Google Play Developer API ilə təsdiqlənir (client-in bəyanına güvənilmir).
    /// </summary>
    Task<SubscriptionResponse> VerifyGooglePurchaseAsync(int userId, VerifyGooglePurchaseRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Google RTDN (Pub/Sub push) mesajını emal edir: purchaseToken-i yenidən Google-dan doğrulayıb
    /// müvafiq istifadəçinin abunəliyini yeniləyir. Mesaj artıq emal olunubsa heç nə etmir (idempotent).
    /// </summary>
    Task ProcessGooglePlayNotificationAsync(string base64Data, string messageId, CancellationToken cancellationToken = default);
}
