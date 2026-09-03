using EssayChecker.Api.Admin;
using EssayChecker.Application.Admin;
using EssayChecker.Application.DTOs.Admin;
using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EssayChecker.Api.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public class UsersModel : PageModel
{
    private readonly IAdminReportRepository _reports;

    public UsersModel(IAdminReportRepository reports)
    {
        _reports = reports;
    }

    public AdminPeriod Period { get; private set; } = AdminPeriod.All;
    public string? Search { get; private set; }
    public string Sort { get; private set; } = "newest";
    public AdminUserListResponse Result { get; private set; } = null!;

    public async Task OnGetAsync(
        AdminPeriod period = AdminPeriod.All,
        string? search = null,
        string sort = "newest",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        Period = period;
        Search = search;
        Sort = sort;

        const int pageSize = 50;
        page = page < 1 ? 1 : page;

        // period burada QEYDİYYAT tarixinə görə süzür (All olduqda süzgəc tətbiq olunmur).
        var (fromUtc, toUtc) = AdminPeriodRange.Resolve(period);
        DateTime? from = period == AdminPeriod.All ? null : fromUtc;
        DateTime? to = period == AdminPeriod.All ? null : toUtc;

        Result = await _reports.GetUsersAsync(
            from, to, search,
            sortByEssays: string.Equals(sort, "essays", StringComparison.OrdinalIgnoreCase),
            page, pageSize, cancellationToken);
    }
}
