using System.Text.Json.Serialization;

namespace EssayChecker.Infrastructure.GooglePlay;

/// <summary>
/// Google Real-time Developer Notifications (RTDN) — Pub/Sub mesajının base64 "data" hissəsinin
/// dekodlanmış JSON formatı. Sxem illərdir sabitdir (developer.android.com/google/play/billing/rtdn-reference).
/// </summary>
internal sealed class GoogleRtdnEnvelope
{
    public string? PackageName { get; set; }

    public string? EventTimeMillis { get; set; }

    [JsonPropertyName("subscriptionNotification")]
    public GoogleSubscriptionNotification? SubscriptionNotification { get; set; }

    /// <summary>Play Console-un "Send test notification" düyməsi ilə gələn boş test payload-ı.</summary>
    [JsonPropertyName("testNotification")]
    public object? TestNotification { get; set; }
}

internal sealed class GoogleSubscriptionNotification
{
    public int NotificationType { get; set; }

    public string? PurchaseToken { get; set; }

    /// <summary>Google-un "subscriptionId" adlandırdığı sahə — bizim ProductId-yə uyğundur.</summary>
    public string? SubscriptionId { get; set; }
}

/// <summary>RTDN notificationType kodları (sabit, illərdir dəyişməyib).</summary>
internal static class GoogleRtdnNotificationType
{
    public const int Recovered = 1;
    public const int Renewed = 2;
    public const int Canceled = 3;
    public const int Purchased = 4;
    public const int OnHold = 5;
    public const int InGracePeriod = 6;
    public const int Restarted = 7;
    public const int PriceChangeConfirmed = 8;
    public const int Deferred = 9;
    public const int Paused = 10;
    public const int PauseScheduleChanged = 11;
    public const int Revoked = 12;
    public const int Expired = 13;
}
