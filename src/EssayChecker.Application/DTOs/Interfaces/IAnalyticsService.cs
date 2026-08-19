using EssayChecker.Application.DTOs.Analytics;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>
/// Şagird inkişafı / zəif tərəflər hesabatları. Qrup və şagird ID-ləri hər çağırışda
/// müəllimə aidliyinə görə yoxlanır; yad ID null qaytarır (controller 404 edir).
/// </summary>
public interface IAnalyticsService
{
    /// <summary>Müəllimin ümumi paneli — bütün esseləri üzrə icmal.</summary>
    Task<OverviewAnalyticsResponse> GetOverviewAsync(int teacherId, CancellationToken cancellationToken = default);

    /// <summary>Qrup icmalı + şagird sıralaması. Qrup müəllimə aid deyilsə null.</summary>
    Task<GroupAnalyticsResponse?> GetGroupAsync(int teacherId, int groupId, CancellationToken cancellationToken = default);

    /// <summary>Şagird profili: bal trendi, səhv profili, təkrarlanan zəif tərəflər.</summary>
    Task<StudentAnalyticsResponse?> GetStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default);
}
