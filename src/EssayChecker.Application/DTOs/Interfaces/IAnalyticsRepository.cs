using EssayChecker.Application.DTOs.Analytics;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>
/// Analitika üçün xam sətirlər. Aqreqasiya (orta, pay, sıralama) qəsdən burada deyil,
/// AnalyticsService-də edilir — belə olduqda eyni sətirlərdən həm şagird, həm qrup, həm də
/// ümumi hesabat eyni riyaziyyatla qurulur.
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>
    /// Müəllimin esselərinin bal/səhv sətirləri. <paramref name="studentId"/> və
    /// <paramref name="groupId"/> filtrdir; ikisi də null olduqda müəllimin BÜTÜN esseləri
    /// (şagird seçilməyənlər də daxil) qayıdır.
    /// </summary>
    Task<IReadOnlyList<EssayAnalyticsRow>> GetRowsAsync(
        int teacherId, int? studentId, int? groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Son <paramref name="take"/> essenin AI rəyi (zəif tərəflər / tövsiyələr). Rəy JSON
    /// sütundadır, ona görə say məhdudlaşdırılır — bütün tarixçəni oxumaq baha başa gələr.
    /// </summary>
    Task<IReadOnlyList<FeedbackRow>> GetRecentFeedbackAsync(
        int teacherId, int? studentId, int? groupId, int take, CancellationToken cancellationToken = default);
}
