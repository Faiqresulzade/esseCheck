using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.Subscriptions;

/// <summary>
/// Plan qaydalarının tək mərkəzi (OCP: yeni plan = enum + burada qayda).
/// </summary>
public static class PlanPolicy
{
    /// <summary>Gündəlik text esse limiti. null = limitsiz.</summary>
    public static int? DailyTextLimit(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Free => 1,
        SubscriptionPlan.Pro => null,
        SubscriptionPlan.ProPlus => null,
        _ => 1
    };

    public static bool UnlimitedText(SubscriptionPlan plan) => DailyTextLimit(plan) is null;

    /// <summary>OCR (şəkildən oxu) yalnız Pro Plus üçün.</summary>
    public static bool CanUseOcr(SubscriptionPlan plan) => plan == SubscriptionPlan.ProPlus;
}
