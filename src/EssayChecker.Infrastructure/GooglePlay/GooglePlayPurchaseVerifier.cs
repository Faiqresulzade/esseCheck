using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Subscriptions;
using EssayChecker.Application.Settings;
using Google;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.GooglePlay;

/// <summary>
/// Google Play Developer API (Purchases.Subscriptions v1) ilə satınalmanı birbaşa Google-dan
/// təsdiqləyir. Client-in göndərdiyi productId/purchaseToken-ə güvənilmir — həqiqət mənbəyi Google-dur.
/// </summary>
internal sealed class GooglePlayPurchaseVerifier : IGooglePlayPurchaseVerifier, IDisposable
{
    private readonly GooglePlaySettings _settings;
    private readonly Lazy<AndroidPublisherService> _service;

    public GooglePlayPurchaseVerifier(IOptions<GooglePlaySettings> options)
    {
        _settings = options.Value;
        _service = new Lazy<AndroidPublisherService>(BuildService);
    }

    public async Task<GooglePurchaseState> VerifyAsync(
        string productId,
        string purchaseToken,
        CancellationToken cancellationToken = default)
    {
        SubscriptionPurchase purchase;
        try
        {
            purchase = await _service.Value.Purchases.Subscriptions
                .Get(_settings.PackageName, productId, purchaseToken)
                .ExecuteAsync(cancellationToken);
        }
        catch (GoogleApiException ex)
        {
            throw new InvalidOperationException($"Google Play satınalması təsdiqlənmədi: {ex.Message}", ex);
        }

        var expiryUtc = purchase.ExpiryTimeMillis.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(purchase.ExpiryTimeMillis.Value).UtcDateTime
            : DateTime.UtcNow;

        return new GooglePurchaseState(
            IsActive: expiryUtc > DateTime.UtcNow,
            ExpiryUtc: expiryUtc,
            AutoRenewing: purchase.AutoRenewing ?? false,
            // 0 = ACKNOWLEDGEMENT_STATE_PENDING, 1 = ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED
            IsAcknowledged: purchase.AcknowledgementState == 1);
    }

    public async Task AcknowledgeAsync(
        string productId,
        string purchaseToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _service.Value.Purchases.Subscriptions
                .Acknowledge(new SubscriptionPurchasesAcknowledgeRequest(), _settings.PackageName, productId, purchaseToken)
                .ExecuteAsync(cancellationToken);
        }
        catch (GoogleApiException ex)
        {
            throw new InvalidOperationException($"Google Play satınalması acknowledge edilmədi: {ex.Message}", ex);
        }
    }

    private AndroidPublisherService BuildService()
    {
        if (!_settings.IsConfigured)
        {
            throw new InvalidOperationException(
                "Google Play Billing hələ konfiqurasiya edilməyib (PackageName / ServiceAccountJsonPath / Products). " +
                "appsettings.json-da \"GooglePlay\" bölməsini doldurun.");
        }

        var credential = GoogleCredential.FromFile(_settings.ServiceAccountJsonPath)
            .CreateScoped(AndroidPublisherService.ScopeConstants.Androidpublisher);

        return new AndroidPublisherService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = _settings.ApplicationName
        });
    }

    public void Dispose()
    {
        if (_service.IsValueCreated)
            _service.Value.Dispose();
    }
}
