using EssayChecker.Application.DTOs.Subscriptions;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.Subscriptions;

/// <summary>Planlar ekranı üçün plan kataloqu (qiymət + xüsusiyyətlər).</summary>
public static class PlanCatalog
{
    public static IReadOnlyList<PlanInfoResponse> All { get; } = new[]
    {
        new PlanInfoResponse(
            SubscriptionPlan.Free, "Free", 0m, "AZN", "ay",
            UnlimitedText: false, DailyTextLimit: 1, Ocr: false,
            Features: new[]
            {
                "Gündə 1 esse şansı (mətnlə yaz)",
                "Tarixçə (pulsuz)"
            }),
        new PlanInfoResponse(
            SubscriptionPlan.Pro, "Pro", 2.99m, "USD", "ay",
            UnlimitedText: true, DailyTextLimit: null, Ocr: false,
            Features: new[]
            {
                "Limitsiz esse (mətnlə yaz)",
                "Tarixçə (pulsuz)"
            }),
        new PlanInfoResponse(
            SubscriptionPlan.ProPlus, "Pro Plus", 5.99m, "USD", "ay",
            UnlimitedText: true, DailyTextLimit: null, Ocr: true,
            Features: new[]
            {
                "Limitsiz esse (mətnlə yaz)",
                "Limitsiz esse (şəkildən oxu)",
                "Tarixçə (pulsuz)"
            })
    };
}
