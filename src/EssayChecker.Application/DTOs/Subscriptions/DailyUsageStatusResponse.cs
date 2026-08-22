using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Subscriptions;

/// <summary>
/// Əsas səhifədəki "Gündəlik pulsuz şans" bloku üçün — mətn və şəkil eyni sayğaca daxildir.
/// Dərs (mövzu izahı) sayğacı isə AYRIDIR: Lesson* sahələri esse sahələrindən asılı deyil.
/// </summary>
public sealed record DailyUsageStatusResponse(
    SubscriptionPlan Plan,
    bool Unlimited,
    int? DailyLimit,
    int UsedToday,
    int? Remaining,
    DateTime ResetAtUtc,
    bool LessonUnlimited,
    int? LessonDailyLimit,
    int LessonsUsedToday,
    int? LessonRemaining);
