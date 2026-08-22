using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

/// <summary>
/// Mövzu izahı (dərs). Yalnız yaratma AI çağırır və gündəlik dərs limitinə (esse limitindən
/// ayrı) sayılır — oxuma və silmə limitsizdir.
/// </summary>
[Authorize]
[Route("api/lessons")]
[ApiController]
public class LessonsController : ApiControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly ITeachingService _teachingService;

    public LessonsController(ILessonService lessonService, ITeachingService teachingService)
    {
        _lessonService = lessonService;
        _teachingService = teachingService;
    }

    /// <summary>Mövzu üzrə dərs yaradır (6-8 slayd + 3 suallıq mini test).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLessonRequest request, CancellationToken cancellationToken)
    {
        // Şagird seçilibsə sahibliyi yoxlanır; sinif sorğuda yoxdursa şagirdin kartından götürülür.
        var student = await ResolveStudentGradeAsync(request.StudentId, cancellationToken);
        if (student.Denied is not null)
            return student.Denied;

        var grade = request.Grade ?? student.Grade;
        if (grade is null)
            return BadRequest(new { message = "Sinif seçilməlidir." });

        var result = await _lessonService.CreateAsync(UserId, request, grade.Value, cancellationToken);

        return result.Outcome switch
        {
            CreateLessonOutcome.Created or CreateLessonOutcome.Reused => Ok(result.Lesson),
            CreateLessonOutcome.InvalidTopic => UnprocessableEntity(new { message = result.Error }),
            _ => StatusCode(StatusCodes.Status429TooManyRequests, new { message = result.Error })
        };
    }

    /// <summary>
    /// Saxlanmış dərslər (səhifələnmiş). Slaydların məzmunu qaytarılmır — yalnız slideCount.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int? studentId,
        [FromQuery] int? groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var history = await _lessonService.GetHistoryAsync(
            UserId, search, studentId, groupId, page, pageSize, cancellationToken);

        return Ok(history);
    }

    /// <summary>Tək dərsin tam məzmunu — limit xərcləmir.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.GetByIdAsync(UserId, id, cancellationToken);
        return lesson is null ? NotFound(new { message = "Dərs tapılmadı." }) : Ok(lesson);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _lessonService.DeleteAsync(UserId, id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { message = "Dərs tapılmadı." });
    }

    /// <summary>
    /// Şagird göndərilibsə onun bu istifadəçiyə aid və silinməmiş olduğunu yoxlayır. Yad (və ya
    /// mövcud olmayan) id "tapılmadı" kimi rədd edilir — mövcudluq faktı sızmır. Şagird
    /// verilməyibsə heç bir sorğu getmir (şagird seçimi opsionaldır).
    /// </summary>
    private async Task<(IActionResult? Denied, GradeLevel? Grade)> ResolveStudentGradeAsync(
        int? studentId, CancellationToken cancellationToken)
    {
        if (studentId is null)
            return (null, null);

        var student = await _teachingService.GetStudentAsync(UserId, studentId.Value, cancellationToken);
        return student is null
            ? (BadRequest(new { message = "Şagird tapılmadı." }), null)
            : (null, student.Grade);
    }
}
