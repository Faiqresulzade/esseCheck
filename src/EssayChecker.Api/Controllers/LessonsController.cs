using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

/// <summary>
/// Mövzu izahı (dərs) — ORTAQ kitabxana. Bir müəllimin yaratdığı dərsi bütün müəllimlər görür və
/// limitsiz oxuyur; gündəlik limit yalnız kitabxanada OLMAYAN yeni mövzu yaradarkən tutulur.
/// Silmə endpoint-i qəsdən yoxdur — dərs ortaq resursdur.
/// </summary>
[Authorize]
[Route("api/lessons")]
[ApiController]
public class LessonsController : ApiControllerBase
{
    private readonly ILessonService _lessonService;

    public LessonsController(ILessonService lessonService)
    {
        _lessonService = lessonService;
    }

    /// <summary>
    /// Mövzu üzrə dərs açır. Mövzu kitabxanada varsa hazır dərs qaytarılır (limit toxunulmur),
    /// yoxdursa AI ilə yaradılır və gündəlik limit xərclənir.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLessonRequest request, CancellationToken cancellationToken)
    {
        var result = await _lessonService.CreateAsync(UserId, request, request.Grade!.Value, cancellationToken);

        return result.Outcome switch
        {
            CreateLessonOutcome.Created or CreateLessonOutcome.AlreadyInLibrary => Ok(result.Lesson),
            CreateLessonOutcome.InvalidTopic => UnprocessableEntity(new { message = result.Error }),
            _ => StatusCode(StatusCodes.Status429TooManyRequests, new { message = result.Error })
        };
    }

    /// <summary>
    /// Kitabxana: bütün müəllimlərin yaratdığı dərslər (səhifələnmiş). Slaydların məzmunu
    /// qaytarılmır — yalnız slideCount. <paramref name="mine"/> ilə yalnız özününküləri süzün.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Library(
        [FromQuery] string? search,
        [FromQuery] GradeLevel? grade,
        [FromQuery] bool mine = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var library = await _lessonService.GetLibraryAsync(
            UserId, search, grade, mine, page, pageSize, cancellationToken);

        return Ok(library);
    }

    /// <summary>Tək dərsin tam məzmunu — kim yaratmasından asılı olmayaraq açıqdır, limit xərcləmir.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var lesson = await _lessonService.GetByIdAsync(UserId, id, cancellationToken);
        return lesson is null ? NotFound(new { message = "Dərs tapılmadı." }) : Ok(lesson);
    }
}
