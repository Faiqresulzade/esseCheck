using EssayChecker.Application.DTOs.Subscriptions;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.Subscriptions;

/// <summary>Planlar ekranı üçün plan kataloqu (qiymət + xüsusiyyətlər).</summary>
public static class PlanCatalog
{
    public static IReadOnlyList<PlanInfoResponse> All { get; } = new[]
    {
        new PlanInfoResponse(
            SubscriptionPlan.Free, "Free", 0m, "USD", "ay",
            Unlimited: false, DailyLimit: 1,
            Features: new[]
            {
                "Gündə 1 esse şansı (mətnlə və ya şəkillə)",
                "Tarixçə (pulsuz)"
            }),
        new PlanInfoResponse(
            SubscriptionPlan.Pro, "Pro", 2.99m, "USD", "ay",
            Unlimited: false, DailyLimit: 10,
            Features: new[]
            {
                "Gündə 10 esse şansı (mətnlə və ya şəkillə)",
                "Tarixçə (pulsuz)"
            }),
        new PlanInfoResponse(
            SubscriptionPlan.ProPlus, "Pro Plus", 5.99m, "USD", "ay",
            Unlimited: true, DailyLimit: null,
            Features: new[]
            {
                "Limitsiz esse (mətnlə və ya şəkillə)",
                "Tarixçə (pulsuz)"
            })
    };
}
