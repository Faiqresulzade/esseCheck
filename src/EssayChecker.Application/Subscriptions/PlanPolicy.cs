using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.Subscriptions;

/// <summary>
/// Plan qaydalarının tək mərkəzi (OCP: yeni plan = enum + burada qayda). Mətnlə və şəkillə
/// (OCR) yoxlama arasında fərq qoyulmur — hər ikisi eyni gündəlik limitə sayılır.
/// </summary>
public static class PlanPolicy
{
    /// <summary>
    /// Gündəlik ümumi yoxlama limiti (mətn + şəkil birlikdə).
    ///
    /// Premium-un 40-ı marketinq mətnində "limitsiz esse yoxlama" kimi təqdim olunur, amma
    /// backend-də HƏQİQİ ədəd kimi tutulur — bu, qəsdən "fair-use" sərhədidir (real AI xərcini
    /// ölçüb hesablanıb, bax CLAUDE.md "Subscriptions"): 40/gün ən ağır real istifadəçini belə
    /// əhatə edir, amma ehtiyatsız/bot istifadədən qorunur. Əgər gələcəkdə bu ədəd dəyişsə,
    /// yalnız burada dəyişdirin — DailyUsageStatusResponse avtomatik doğru rəqəmi göstərəcək.
    /// </summary>
    public static int? DailyLimit(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Free => 1,
        SubscriptionPlan.Pro => 10,
        SubscriptionPlan.ProPlus => 20,
        SubscriptionPlan.Premium => 40,
        _ => 1
    };

    public static bool IsUnlimited(SubscriptionPlan plan) => DailyLimit(plan) is null;

    /// <summary>
    /// Gündəlik YENİ dərs yaratma limiti. Esse limitindən TAM AYRIDIR — Free istifadəçi bir gündə
    /// həm 1 esse, həm də 1 dərs ala bilər.
    ///
    /// Planlar üzrə fərqlidir (yüksək plan = daha çox yeni mövzu yaratma haqqı), amma bu, dərsin
    /// OXUNMASINA aid deyil: dərslər ortaq kitabxanadadır, istənilən plan başqalarının yaratdığı
    /// istənilən sayda dərsi LİMİTSİZ oxuyur. Bu limit yalnız yeni AI çağırışını (real xərci)
    /// məhdudlaşdırır.
    /// </summary>
    public static int? LessonDailyLimit(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Free => 1,
        SubscriptionPlan.Pro => 1,
        SubscriptionPlan.ProPlus => 2,
        SubscriptionPlan.Premium => 4,
        _ => 1
    };

    public static bool IsLessonUnlimited(SubscriptionPlan plan) => LessonDailyLimit(plan) is null;
}
