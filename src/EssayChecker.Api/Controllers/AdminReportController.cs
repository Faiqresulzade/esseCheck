using System.Security.Cryptography;
using System.Text;
using EssayChecker.Application.Admin;
using EssayChecker.Application.DTOs.Admin;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EssayChecker.Api.Controllers;

/// <summary>
/// Sahibkar üçün oxu-yalnız statistika. BÜTÜN istifadəçilərin adı/e-maili qaytarıldığı üçün adi
/// JWT ilə qorunmur — ayrıca gizli açar tələb olunur (bax <see cref="AdminSettings"/>), əks halda
/// hər qeydiyyatlı istifadəçi hamının şəxsi məlumatını görə bilərdi.
///
/// Açar konfiqurasiya olunmayıbsa endpoint-lər ümumiyyətlə mövcud deyil (404) — funksiyanın
/// təsadüfən açıq qalması mümkün deyil.
/// </summary>
[AllowAnonymous]
[Route("api/admin")]
[ApiController]
public class AdminReportController : ControllerBase
{
    private readonly IAdminReportRepository _reports;
    private readonly AdminSettings _settings;

    public AdminReportController(IAdminReportRepository reports, IOptions<AdminSettings> settings)
    {
        _reports = reports;
        _settings = settings.Value;
    }

    /// <summary>Ümumi mənzərə: qeydiyyat, abunə, esse, məzmun və server rəqəmləri.</summary>
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(
        [FromQuery] string? secret,
        [FromQuery] AdminPeriod period = AdminPeriod.Today,
        CancellationToken cancellationToken = default)
    {
        if (!IsAuthorized(secret))
            return NotFound();

        var (fromUtc, toUtc) = AdminPeriodRange.Resolve(period);
        return Ok(await _reports.GetOverviewAsync(period, fromUtc, toUtc, cancellationToken));
    }

    /// <summary>
    /// İstifadəçi siyahısı. <paramref name="period"/> QEYDİYYAT tarixinə görə süzür
    /// (today / yesterday / last7days / last30days / all).
    /// <paramref name="sort"/>: "newest" (defolt) və ya "essays" (ən çox esse yoxlayan əvvəldə).
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> Users(
        [FromQuery] string? secret,
        [FromQuery] AdminPeriod period = AdminPeriod.All,
        [FromQuery] string? sort = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!IsAuthorized(secret))
            return NotFound();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var (fromUtc, toUtc) = AdminPeriodRange.Resolve(period);
        DateTime? registeredFrom = period == AdminPeriod.All ? null : fromUtc;
        DateTime? registeredTo = period == AdminPeriod.All ? null : toUtc;

        var sortByEssays = string.Equals(sort, "essays", StringComparison.OrdinalIgnoreCase);

        return Ok(await _reports.GetUsersAsync(
            registeredFrom, registeredTo, search, sortByEssays, page, pageSize, cancellationToken));
    }

    /// <summary>Dövr ərzində esse yoxlayanlar və hər birinin neçə dəfə yoxladığı.</summary>
    [HttpGet("activity")]
    public async Task<IActionResult> Activity(
        [FromQuery] string? secret,
        [FromQuery] AdminPeriod period = AdminPeriod.Today,
        CancellationToken cancellationToken = default)
    {
        if (!IsAuthorized(secret))
            return NotFound();

        var (fromUtc, toUtc) = AdminPeriodRange.Resolve(period);
        return Ok(await _reports.GetActivityAsync(period, fromUtc, toUtc, cancellationToken));
    }

    /// <summary>
    /// Açar yoxlaması. Konfiqurasiya olunmayıbsa həmişə false — endpoint bağlı qalır.
    /// Müqayisə sabit vaxtlıdır (SubscriptionController-dəki RTDN yoxlaması ilə eyni prinsip).
    /// </summary>
    private bool IsAuthorized(string? secret)
    {
        if (!_settings.IsConfigured || string.IsNullOrEmpty(secret))
            return false;

        var provided = Encoding.UTF8.GetBytes(secret);
        var expected = Encoding.UTF8.GetBytes(_settings.ApiKey);

        return provided.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(provided, expected);
    }
}
