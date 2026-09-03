using EssayChecker.Api.Admin;
using EssayChecker.Application.Admin;
using EssayChecker.Application.DTOs.Admin;
using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EssayChecker.Api.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public class IndexModel : PageModel
{
    private readonly IAdminReportRepository _reports;

    public IndexModel(IAdminReportRepository reports)
    {
        _reports = reports;
    }

    public AdminPeriod Period { get; private set; } = AdminPeriod.Today;
    public AdminOverviewResponse Overview { get; private set; } = null!;
    public AdminActivityResponse Activity { get; private set; } = null!;

    public async Task OnGetAsync(AdminPeriod period = AdminPeriod.Today, CancellationToken cancellationToken = default)
    {
        Period = period;
        var (fromUtc, toUtc) = AdminPeriodRange.Resolve(period);

        Overview = await _reports.GetOverviewAsync(period, fromUtc, toUtc, cancellationToken);
        Activity = await _reports.GetActivityAsync(period, fromUtc, toUtc, cancellationToken);
    }
}
