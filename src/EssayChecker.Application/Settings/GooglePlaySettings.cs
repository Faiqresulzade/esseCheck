using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.Settings;

/// <summary>
/// Google Play Billing konfiqurasiyası. Qəsdən startup-da validasiya olunmur (bax Program.cs) —
/// Play Console tam qurulana qədər boş/placeholder dəyərlərlə tətbiqin qalan hissəsi işləməlidir.
/// Konfiqurasiyanın tamlığı yalnız faktiki Google çağırışında (verify/RTDN) yoxlanılır.
/// </summary>
public sealed class GooglePlaySettings
{
    public const string SectionName = "GooglePlay";

    /// <summary>Tətbiqin package name-i (məs. az.essaycheck.app).</summary>
    public string PackageName { get; set; } = string.Empty;

    /// <summary>Service account JSON açar faylının yolu (Play Console-dan endirilən).</summary>
    public string ServiceAccountJsonPath { get; set; } = string.Empty;

    public string ApplicationName { get; set; } = "EssayCheck AI";

    /// <summary>Google Play subscription/product ID → daxili plan uyğunlaşdırması.</summary>
    public Dictionary<string, SubscriptionPlan> Products { get; set; } = new();

    /// <summary>RTDN (Pub/Sub push) endpoint-inin doğrulanması üçün paylaşılan sirr (query string ilə göndərilir).</summary>
    public string RtdnSharedSecret { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PackageName) &&
        !string.IsNullOrWhiteSpace(ServiceAccountJsonPath) &&
        Products.Count > 0;
}
