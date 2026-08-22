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

    /// <summary>
    /// Gündəlik mövzu izahı (dərs) limiti. Esse limitindən TAM AYRIDIR — yəni Free istifadəçi
    /// bir gündə həm 1 esse, həm də 1 dərs ala bilər. null = limitsiz.
    /// </summary>
    public static int? LessonDailyLimit(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Free => 1,
        SubscriptionPlan.Pro => 5,
        SubscriptionPlan.ProPlus => null,
        _ => 1
    };

    public static bool IsLessonUnlimited(SubscriptionPlan plan) => LessonDailyLimit(plan) is null;
}
