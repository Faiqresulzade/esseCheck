using EssayChecker.Api.Admin;
using EssayChecker.Application.Admin;
using EssayChecker.Application.DTOs.Admin;
using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EssayChecker.Api.Pages.Admin;

[Authorize(Policy = AdminAuth.Policy)]
public class ActivityModel : PageModel
{
    private readonly IAdminReportRepository _reports;

    public ActivityModel(IAdminReportRepository reports)
    {
        _reports = reports;
    }

    public AdminPeriod Period { get; private set; } = AdminPeriod.Today;
    public AdminActivityResponse Result { get; private set; } = null!;

    public async Task OnGetAsync(AdminPeriod period = AdminPeriod.Today, CancellationToken cancellationToken = default)
    {
        Period = period;
        var (fromUtc, toUtc) = AdminPeriodRange.Resolve(period);
        Result = await _reports.GetActivityAsync(period, fromUtc, toUtc, cancellationToken);
    }
}
