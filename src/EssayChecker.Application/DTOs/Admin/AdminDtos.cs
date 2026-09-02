using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Admin;

/// <summary>
/// Hesabat dövrü. Sərhədlər UTC-də yox, AZƏRBAYCAN vaxtında (UTC+4) hesablanır — "bu gün"
/// deyəndə sahibkarın gördüyü təqvim günü nəzərdə tutulur, serverin UTC günü yox.
/// </summary>
public enum AdminPeriod
{
    Today = 0,
    Yesterday = 1,
    Last7Days = 2,
    Last30Days = 3,
    All = 4
}

public sealed record AdminPlanCount(SubscriptionPlan Plan, int Count);

public sealed record AdminPlatformCount(SubscriptionPlatform Platform, int Count);

public sealed record AdminNamedCount(string Name, int Count);

/// <summary>ActiveInPeriod = dövr ərzində ən azı 1 esse yoxlayan istifadəçi sayı.</summary>
public sealed record AdminUserStats(
    int TotalAllTime,
    int NewInPeriod,
    int ActiveInPeriod,
    int SoftDeleted);

/// <summary>
/// Abunə mənzərəsi. <see cref="RealPurchasesActive"/> yeganə GƏLİR gətirən ədəddir — qalanları
/// pulsuz sınaq (Trial) və ya əl ilə verilmiş qeydlərdir.
/// </summary>
public sealed record AdminSubscriptionStats(
    int ActiveTotal,
    int RealPurchasesActive,
    int RealPurchasesInPeriod,
    int TrialsActive,
    IReadOnlyList<AdminPlanCount> ByPlan,
    IReadOnlyList<AdminPlatformCount> ByPlatform);

public sealed record AdminEssayStats(
    int TotalAllTime,
    int InPeriod,
    IReadOnlyList<AdminNamedCount> ByGrade,
    IReadOnlyList<AdminNamedCount> BySource);

public sealed record AdminContentStats(
    int LessonsTotal,
    int LessonsInPeriod,
    int ActiveGroups,
    int ActiveStudents,
    int DeviceTrialsUsed);

public sealed record AdminHealthStats(
    int RequestsInPeriod,
    int ServerErrorsInPeriod,
    int RateLimitedInPeriod);

public sealed record AdminOverviewResponse(
    AdminPeriod Period,
    DateTime FromUtc,
    DateTime ToUtc,
    AdminUserStats Users,
    AdminSubscriptionStats Subscriptions,
    AdminEssayStats Essays,
    AdminContentStats Content,
    AdminHealthStats Health);

/// <summary>
/// Bir istifadəçi sətri. Plan bazadakı REAL vəziyyətdir (Testing:ForceProPlusForAllUsers bayrağı
/// nəzərə alınmır — sahibkar üçün lazım olan həqiqi abunə mənzərəsidir, effektiv hüquqlar yox).
/// IsPaying: true = real Google Play alışı (gəlir), false = trial və ya əl ilə verilmiş.
/// </summary>
public sealed record AdminUserItem(
    int Id,
    string FullName,
    string? Email,
    DateTime CreatedAt,
    DateTime? LastLoginDate,
    bool IsDeleted,
    SubscriptionPlan Plan,
    SubscriptionPlatform? Platform,
    DateTime? SubscriptionEndDate,
    bool IsPaying,
    int EssayCount,
    DateTime? LastEssayAt,
    int LessonCount,
    int GroupCount,
    int StudentCount);

public sealed record AdminUserListResponse(
    IReadOnlyList<AdminUserItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

/// <summary>Dövr ərzində esse yoxlayan bir istifadəçi və neçə dəfə yoxladığı.</summary>
public sealed record AdminActivityItem(
    int UserId,
    string FullName,
    string? Email,
    int EssayCount,
    DateTime FirstEssayAt,
    DateTime LastEssayAt);

public sealed record AdminActivityResponse(
    AdminPeriod Period,
    DateTime FromUtc,
    DateTime ToUtc,
    int TotalUsers,
    int TotalEssays,
    IReadOnlyList<AdminActivityItem> Items);
