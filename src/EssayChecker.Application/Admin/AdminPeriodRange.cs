using EssayChecker.Application.DTOs.Admin;

namespace EssayChecker.Application.Admin;

/// <summary>
/// Hesabat dövrünün UTC sərhədləri. Bazadakı bütün tarixlər UTC-dir, amma sahibkar "bu gün"
/// deyəndə Azərbaycan təqvim gününü nəzərdə tutur — ona görə sərhədlər UTC+4-ə görə hesablanır.
///
/// Sabit +4 istifadə olunur (Azərbaycan 2016-dan yay vaxtına keçmir), ona görə TimeZoneInfo
/// axtarışına ehtiyac yoxdur — o, Linux konteynerində tzdata olmadıqda sınır.
/// </summary>
public static class AdminPeriodRange
{
    private static readonly TimeSpan AzOffset = TimeSpan.FromHours(4);

    public static (DateTime FromUtc, DateTime ToUtc) Resolve(AdminPeriod period)
    {
        var nowUtc = DateTime.UtcNow;
        var azNow = nowUtc + AzOffset;
        var azTodayStart = azNow.Date;

        var (azFrom, azTo) = period switch
        {
            AdminPeriod.Today => (azTodayStart, azTodayStart.AddDays(1)),
            AdminPeriod.Yesterday => (azTodayStart.AddDays(-1), azTodayStart),
            AdminPeriod.Last7Days => (azTodayStart.AddDays(-6), azTodayStart.AddDays(1)),
            AdminPeriod.Last30Days => (azTodayStart.AddDays(-29), azTodayStart.AddDays(1)),
            _ => (DateTime.MinValue, azTodayStart.AddDays(1))
        };

        var fromUtc = azFrom == DateTime.MinValue ? DateTime.MinValue : azFrom - AzOffset;
        var toUtc = azTo - AzOffset;

        return (fromUtc, toUtc);
    }
}
