using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class EssayController : ApiControllerBase
{
    private readonly IEssayService _essayService;
    private readonly IUsageLimitService _usageLimitService;

    public EssayController(IEssayService essayService, IUsageLimitService usageLimitService)
    {
        _essayService = essayService;
        _usageLimitService = usageLimitService;
    }

    /// <summary>
    /// Mətni AI ilə qiymətləndirir və tarixçəyə yazır (gündəlik limit yoxlanır). Yalnız 11-ci
    /// sinif — 9-cu sinif DİM formatına görə tam şəkil-əsaslıdır, bax /evaluate/grade9-images.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateEssayRequest request, CancellationToken cancellationToken)
    {
        if (request.Grade == GradeLevel.Grade9)
        {
            return BadRequest(new
            {
                message = "9-cu sinif üçün esse yalnız 3 promt-şəkli ilə göndərilməlidir: /api/Essay/evaluate/grade9-images."
            });
        }

        var denied = await CheckUsageAsync(hasImages: false, cancellationToken);
        if (denied is not null)
            return denied;

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
        var denied = await CheckUsageAsync(hasImages: true, cancellationToken);
        if (denied is not null)
            return denied;

        if (image is null || image.Length == 0)
            return BadRequest(new { message = "Şəkil tələb olunur." });

        if (!IsImage(image))
            return BadRequest(new { message = "Yalnız şəkil faylı qəbul olunur." });

        await using var stream = image.OpenReadStream();
        var result = await _essayService.ReadImageAsync(stream, image.ContentType, cancellationToken);

        await _usageLimitService.ConsumeOcrAsync(UserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 9-cu sinif — DİM formatı: tələbəyə verilən promt-şəkilləri (yazı tapşırığı, 0-3 ədəd,
    /// hamısı opsionaldır) + yazdığı esse mətni. Şəkil göndərilməzsə adi mətn limiti (gündəlik),
    /// şəkil göndərilərsə OCR/vision limiti (yalnız Pro Plus) tətbiq olunur — çünki yalnız o
    /// halda vision resursu istifadə olunur. Grade həmişə Grade9-dur, ayrıca sahə göndərilmir.
    /// </summary>
    [HttpPost("evaluate/grade9-images")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> EvaluateGrade9WithImages(
        [FromForm] string text,
        [FromForm] string? title,
        IFormFile? promptImage1,
        IFormFile? promptImage2,
        IFormFile? promptImage3,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { message = "Esse mətni boş ola bilməz." });

        var files = new[] { promptImage1, promptImage2, promptImage3 }
            .Where(f => f is not null && f.Length > 0)
            .Select(f => f!)
            .ToList();

        if (files.Any(f => !IsImage(f)))
            return BadRequest(new { message = "Yalnız şəkil faylları qəbul olunur." });

        var hasImages = files.Count > 0;

        var denied = await CheckUsageAsync(hasImages, cancellationToken);
        if (denied is not null)
            return denied;

        var promptImages = await ToPromptImagesAsync(files, cancellationToken);

        var result = await _essayService.EvaluateGrade9WithImagesAsync(UserId, text, title, promptImages, cancellationToken);
        if (!result.Success)
            return UnprocessableEntity(new { message = result.Error ?? "Göndərilən mətn esse deyil." });

        if (hasImages)
            await _usageLimitService.ConsumeOcrAsync(UserId, cancellationToken);
        else
            await _usageLimitService.ConsumeTextAsync(UserId, cancellationToken);

        return Ok(result.Essay);
    }

    /// <summary>
    /// Şəkil varsa OCR/vision limitini (yalnız Pro Plus), yoxdursa adi gündəlik mətn limitini
    /// yoxlayır. İcazə yoxdursa müvafiq HTTP statuslu cavab qaytarır, varsa null.
    /// </summary>
    private async Task<IActionResult?> CheckUsageAsync(bool hasImages, CancellationToken cancellationToken)
    {
        var decision = hasImages
            ? await _usageLimitService.CheckOcrAsync(UserId, cancellationToken)
            : await _usageLimitService.CheckTextAsync(UserId, cancellationToken);

        if (decision.Allowed)
            return null;

        var statusCode = hasImages ? StatusCodes.Status403Forbidden : StatusCodes.Status429TooManyRequests;
        return StatusCode(statusCode, new { message = decision.Reason });
    }

    private static bool IsImage(IFormFile file) =>
        !string.IsNullOrEmpty(file.ContentType) && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private static async Task<List<PromptImage>> ToPromptImagesAsync(IReadOnlyList<IFormFile> files, CancellationToken cancellationToken)
    {
        var promptImages = new List<PromptImage>(files.Count);
        foreach (var file in files)
        {
            using var memory = new MemoryStream();
            await file.CopyToAsync(memory, cancellationToken);
            promptImages.Add(new PromptImage(memory.ToArray(), file.ContentType));
        }

        return promptImages;
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
