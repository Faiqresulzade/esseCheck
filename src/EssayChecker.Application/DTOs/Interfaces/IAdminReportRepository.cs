using EssayChecker.Application.DTOs.Admin;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>
/// Sahibkar üçün oxu-yalnız hesabat sorğuları. Heç bir yazma əməliyyatı yoxdur — bu endpoint-lər
/// yalnız mövcud vəziyyəti göstərir, dəyişdirmir.
/// </summary>
public interface IAdminReportRepository
{
    Task<AdminOverviewResponse> GetOverviewAsync(
        AdminPeriod period, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <param name="registeredFromUtc">null = bütün vaxtlar; əks halda yalnız bu tarixdən sonra qeydiyyatdan keçənlər.</param>
    /// <param name="sortByEssays">true = ən çox esse yoxlayan əvvəldə; false = ən yeni qeydiyyat əvvəldə.</param>
    Task<AdminUserListResponse> GetUsersAsync(
        DateTime? registeredFromUtc, DateTime? registeredToUtc, string? search, bool sortByEssays,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<AdminActivityResponse> GetActivityAsync(
        AdminPeriod period, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
