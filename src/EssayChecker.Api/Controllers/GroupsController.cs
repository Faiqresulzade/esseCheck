using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Teaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

/// <summary>
/// Müəllimin şagird qrupları. Ayrıca "müəllim" rolu yoxdur — istənilən istifadəçi qrup yarada
/// bilər; ödənişli plan yalnız gündəlik esse limitini artırır.
/// </summary>
[Authorize]
[Route("api/groups")]
[ApiController]
public class GroupsController : ApiControllerBase
{
    private readonly ITeachingService _teachingService;

    public GroupsController(ITeachingService teachingService)
    {
        _teachingService = teachingService;
    }

    /// <summary>Müəllimin bütün qrupları (hər birində silinməmiş şagird sayı ilə).</summary>
    [HttpGet]
    public async Task<IActionResult> GetGroups(CancellationToken cancellationToken) =>
        Ok(await _teachingService.GetGroupsAsync(UserId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] SaveGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await _teachingService.CreateGroupAsync(UserId, request, cancellationToken);
        return result.Success
            ? Ok(result.Group)
            : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> RenameGroup(int id, [FromBody] SaveGroupRequest request, CancellationToken cancellationToken)
    {
        var renamed = await _teachingService.RenameGroupAsync(UserId, id, request, cancellationToken);
        return renamed ? NoContent() : NotFound();
    }

    /// <summary>Qrupu silir (şagirdləri də siyahıdan çıxır). Esse tarixçəsi toxunulmur.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteGroup(int id, CancellationToken cancellationToken)
    {
        var deleted = await _teachingService.DeleteGroupAsync(UserId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Qrupun şagirdləri.</summary>
    [HttpGet("{id:int}/students")]
    public async Task<IActionResult> GetGroupStudents(int id, CancellationToken cancellationToken) =>
        Ok(await _teachingService.GetStudentsAsync(UserId, id, cancellationToken));

    [HttpPost("{id:int}/students")]
    public async Task<IActionResult> AddStudent(int id, [FromBody] SaveStudentRequest request, CancellationToken cancellationToken)
    {
        var result = await _teachingService.CreateStudentAsync(UserId, id, request, cancellationToken);
        return result.Success
            ? Ok(result.Student)
            : BadRequest(new { message = result.Error });
    }
}
