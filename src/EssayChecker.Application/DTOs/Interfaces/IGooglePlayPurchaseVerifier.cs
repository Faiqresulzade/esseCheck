using EssayChecker.Application.DTOs.Subscriptions;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>Google Play Developer API ilə satınalmanı birbaşa Google-dan təsdiqləyir (client-ə güvənmirik).</summary>
public interface IGooglePlayPurchaseVerifier
{
    Task<GooglePurchaseState> VerifyAsync(string productId, string purchaseToken, CancellationToken cancellationToken = default);

    /// <summary>Google 3 gün ərzində acknowledge tələb edir, yoxsa avtomatik geri qaytarır.</summary>
    Task AcknowledgeAsync(string productId, string purchaseToken, CancellationToken cancellationToken = default);
}
