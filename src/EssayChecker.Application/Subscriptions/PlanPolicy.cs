using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.Subscriptions;

/// <summary>
/// Plan qaydalarının tək mərkəzi (OCP: yeni plan = enum + burada qayda). Mətnlə və şəkillə
/// (OCR) yoxlama arasında fərq qoyulmur — hər ikisi eyni gündəlik limitə sayılır.
/// </summary>
public static class PlanPolicy
{
    /// <summary>Gündəlik ümumi yoxlama limiti (mətn + şəkil birlikdə). null = limitsiz.</summary>
    public static int? DailyLimit(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Free => 1,
        SubscriptionPlan.Pro => 10,
        SubscriptionPlan.ProPlus => null,
        _ => 1
    };

    public static bool IsUnlimited(SubscriptionPlan plan) => DailyLimit(plan) is null;
}
