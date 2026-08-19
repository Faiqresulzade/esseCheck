using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Teaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

/// <summary>
/// Şagird kartları. Esse formasındakı şagird droplisti üçün əsas endpoint budur:
/// <c>GET /api/students</c> müəllimin bütün qruplarındakı şagirdləri bir siyahıda qaytarır.
/// </summary>
[Authorize]
[Route("api/students")]
[ApiController]
public class StudentsController : ApiControllerBase
{
    private readonly ITeachingService _teachingService;

    public StudentsController(ITeachingService teachingService)
    {
        _teachingService = teachingService;
    }

    /// <summary>Bütün şagirdlər (droplist). <paramref name="groupId"/> verilsə yalnız o qrupdakılar.</summary>
    [HttpGet]
    public async Task<IActionResult> GetStudents([FromQuery] int? groupId, CancellationToken cancellationToken) =>
        Ok(await _teachingService.GetStudentsAsync(UserId, groupId, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetStudent(int id, CancellationToken cancellationToken)
    {
        var student = await _teachingService.GetStudentAsync(UserId, id, cancellationToken);
        return student is null ? NotFound() : Ok(student);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] SaveStudentRequest request, CancellationToken cancellationToken)
    {
        var updated = await _teachingService.UpdateStudentAsync(UserId, id, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    /// <summary>Şagirdi siyahıdan çıxarır. Esseləri silinmir — inkişaf tarixçəsi qorunur.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStudent(int id, CancellationToken cancellationToken)
    {
        var deleted = await _teachingService.DeleteStudentAsync(UserId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
