using System.Security.Claims;
using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class EssayController : ControllerBase
{
    private readonly IEssayService _essayService;
    private readonly IUsageLimitService _usageLimitService;

    public EssayController(IEssayService essayService, IUsageLimitService usageLimitService)
    {
        _essayService = essayService;
        _usageLimitService = usageLimitService;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Mətni AI ilə qiymətləndirir və tarixçəyə yazır (gündəlik limit yoxlanır).</summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateEssayRequest request, CancellationToken cancellationToken)
    {
        var decision = await _usageLimitService.CheckTextAsync(UserId, cancellationToken);
        if (!decision.Allowed)
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = decision.Reason });

        var result = await _essayService.EvaluateAsync(UserId, request, cancellationToken);
        if (!result.Success)
            return UnprocessableEntity(new { message = result.Error ?? "Göndərilən mətn esse deyil." });

        // Yalnız uğurlu qiymətləndirmədən sonra limiti azaldırıq.
        await _usageLimitService.ConsumeTextAsync(UserId, cancellationToken);
        return Ok(result.Essay);
    }

    /// <summary>Şəkildən mətn oxuyur (OCR) — yalnız Pro Plus. İstifadəçi baxıb düzəldəcək.</summary>
    [HttpPost("ocr")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Ocr(IFormFile image, CancellationToken cancellationToken)
    {
        var decision = await _usageLimitService.CheckOcrAsync(UserId, cancellationToken);
        if (!decision.Allowed)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = decision.Reason });

        if (image is null || image.Length == 0)
            return BadRequest(new { message = "Şəkil tələb olunur." });

        if (string.IsNullOrEmpty(image.ContentType) || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Yalnız şəkil faylı qəbul olunur." });

        await using var stream = image.OpenReadStream();
        var result = await _essayService.ReadImageAsync(stream, image.ContentType, cancellationToken);

        await _usageLimitService.ConsumeOcrAsync(UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Tarixçə siyahısı (səhifələnmiş, axtarış opsional). page ən azı 1, pageSize 1–100 aralığında.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var history = await _essayService.GetHistoryAsync(UserId, search, page, pageSize, cancellationToken);
        return Ok(history);
    }

    /// <summary>Tarixçə detalı.</summary>
    [HttpGet("history/{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var essay = await _essayService.GetByIdAsync(UserId, id, cancellationToken);
        return essay is null ? NotFound() : Ok(essay);
    }

    /// <summary>Tarixçə qeydini silir.</summary>
    [HttpDelete("history/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _essayService.DeleteAsync(UserId, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Bütün tarixçəni silir (Ayarlar → Tarixçəni sil).</summary>
    [HttpDelete("history")]
    public async Task<IActionResult> DeleteAll(CancellationToken cancellationToken)
    {
        var count = await _essayService.DeleteAllAsync(UserId, cancellationToken);
        return Ok(new { deleted = count });
    }
}
