using EssayChecker.Domain.Enums;

namespace EssayChecker.Domain.Entities.Subscriptions;

/// <summary>
/// İstifadəçinin abunəliyi. Google Play Billing inteqrasiyası üçün hazırdır
/// (PurchaseToken, Platform, AutoRenew).
/// </summary>
public class UserSubscription
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public SubscriptionPlan Plan { get; set; }

    public DateTime StartDate { get; set; }

    /// <summary>Bitmə tarixi (UTC). null = müddətsiz.</summary>
    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public bool AutoRenew { get; set; }

    /// <summary>Google Play / App Store satınalma tokeni (server-side təsdiq üçün).</summary>
    public string? PurchaseToken { get; set; }

    /// <summary>PurchaseToken-in SHA-256 hash-i (axtarış üçün — token özü indexlənə bilməyəcək qədər uzun ola bilər).</summary>
    public string? PurchaseTokenHash { get; set; }

    /// <summary>Google Play subscription/product ID (məs. "pro_monthly").</summary>
    public string? ProductId { get; set; }

    /// <summary>Plan yüksəltmə/endirmədə əvvəlki satınalma tokeni (Google-un upgrade zənciri).</summary>
    public string? LinkedPurchaseToken { get; set; }

    public SubscriptionPlatform Platform { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
