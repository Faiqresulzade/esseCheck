using System.ComponentModel.DataAnnotations;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Subscriptions;

public sealed class SubscribeRequest
{
    /// <summary>Yalnız Pro və ya ProPlus (Free defolt plandır).</summary>
    [EnumDataType(typeof(SubscriptionPlan), ErrorMessage = "Plan düzgün deyil.")]
    public SubscriptionPlan Plan { get; set; }

    public SubscriptionPlatform Platform { get; set; } = SubscriptionPlatform.Manual;

    /// <summary>Google Play / App Store satınalma tokeni (gələcək inteqrasiya).</summary>
    [MaxLength(4000)]
    public string? PurchaseToken { get; set; }

    public bool AutoRenew { get; set; }

    /// <summary>Abunəliyin gün sayı (defolt 30).</summary>
    [Range(1, 366, ErrorMessage = "Müddət 1–366 gün aralığında olmalıdır.")]
    public int DurationDays { get; set; } = 30;
}
