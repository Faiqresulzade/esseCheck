using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

/// <summary>
/// Şagird inkişafı hesabatları. Hamısı mövcud esse nəticələrindən hesablanır — əlavə AI
/// çağırışı yoxdur, ona görə gündəlik limitə təsir etmir.
/// </summary>
[Authorize]
[Route("api/analytics")]
[ApiController]
public class AnalyticsController : ApiControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>Ümumi panel: bütün qruplar/şagirdlər üzrə icmal.</summary>
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var overview = await _analyticsService.GetOverviewAsync(UserId, cancellationToken);
        return Ok(overview);
    }

    /// <summary>Qrup icmalı + şagird sıralaması.</summary>
    [HttpGet("groups/{groupId:int}")]
    public async Task<IActionResult> Group(int groupId, CancellationToken cancellationToken)
    {
        var group = await _analyticsService.GetGroupAsync(UserId, groupId, cancellationToken);
        return group is null ? NotFound(new { message = "Qrup tapılmadı." }) : Ok(group);
    }

    /// <summary>Şagird profili: bal trendi, səhv profili, təkrarlanan zəif tərəflər.</summary>
    [HttpGet("students/{studentId:int}")]
    public async Task<IActionResult> Student(int studentId, CancellationToken cancellationToken)
    {
        var student = await _analyticsService.GetStudentAsync(UserId, studentId, cancellationToken);
        return student is null ? NotFound(new { message = "Şagird tapılmadı." }) : Ok(student);
    }
}
