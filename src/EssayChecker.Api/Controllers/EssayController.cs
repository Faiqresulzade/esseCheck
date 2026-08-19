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
    private readonly ITeachingService _teachingService;

    public EssayController(
        IEssayService essayService,
        IUsageLimitService usageLimitService,
        ITeachingService teachingService)
    {
        _essayService = essayService;
        _usageLimitService = usageLimitService;
        _teachingService = teachingService;
    }

    /// <summary>
    /// Mətni AI ilə qiymətləndirir və tarixçəyə yazır (gündəlik limit yoxlanır). Yalnız 11-ci
    /// sinif — 9-cu sinif DİM formatına görə tam şəkil-əsaslıdır, bax /evaluate/grade9-images.
    /// </summary>
    [HttpPost("evaluate")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateEssayRequest request, CancellationToken cancellationToken)
    {
        // Şagird seçilibsə sahibliyi yoxlanır; sinif sorğuda yoxdursa şagirdin kartından götürülür.
        var studentGrade = await ResolveStudentGradeAsync(request.StudentId, cancellationToken);
        if (studentGrade.Denied is not null)
            return studentGrade.Denied;

        var grade = request.Grade ?? studentGrade.Grade;
        if (grade is null)
            return BadRequest(new { message = "Sinif seçilməlidir." });

        if (grade == GradeLevel.Grade9)
        {
            return BadRequest(new
            {
                message = "9-cu sinif üçün esse yalnız 3 promt-şəkli ilə göndərilməlidir: /api/Essay/evaluate/grade9-images."
            });
        }

        var denied = await CheckUsageAsync(hasImages: false, cancellationToken);
        if (denied is not null)
            return denied;

        var result = await _essayService.EvaluateAsync(UserId, request, grade.Value, cancellationToken);
        if (!result.Success)
            return UnprocessableEntity(new { message = result.Error ?? "Göndərilən mətn esse deyil." });

        // Yalnız uğurlu qiymətləndirmədən sonra limiti azaldırıq.
        await _usageLimitService.ConsumeTextAsync(UserId, cancellationToken);
        return Ok(result.Essay);
    }

    /// <summary>Şəkildən mətn oxuyur (OCR) — gündəlik limit mətnlə eyni sayğaca daxildir. İstifadəçi baxıb düzəldəcək.</summary>
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
    /// hamısı opsionaldır) + yazdığı esse mətni. Şəkilli olsun ya olmasın, eyni gündəlik
    /// limitə (bax PlanPolicy.DailyLimit) sayılır. Grade həmişə Grade9-dur, ayrıca sahə
    /// göndərilmir.
    /// </summary>
    [HttpPost("evaluate/grade9-images")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> EvaluateGrade9WithImages(
        [FromForm] string text,
        [FromForm] string? title,
        [FromForm] int? studentId,
        IFormFile? promptImage1,
        IFormFile? promptImage2,
        IFormFile? promptImage3,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { message = "Esse mətni boş ola bilməz." });

        // Sinif burada həmişə Grade9-dur, ona görə yalnız şagirdin sahibliyi yoxlanılır.
        var student = await ResolveStudentGradeAsync(studentId, cancellationToken);
        if (student.Denied is not null)
            return student.Denied;

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

        var result = await _essayService.EvaluateGrade9WithImagesAsync(UserId, text, title, studentId, promptImages, cancellationToken);
        if (!result.Success)
            return UnprocessableEntity(new { message = result.Error ?? "Göndərilən mətn esse deyil." });

        if (hasImages)
            await _usageLimitService.ConsumeOcrAsync(UserId, cancellationToken);
        else
            await _usageLimitService.ConsumeTextAsync(UserId, cancellationToken);

        return Ok(result.Essay);
    }

    /// <summary>
    /// Gündəlik limiti yoxlayır (mətn və şəkil eyni sayğaca daxildir). İcazə yoxdursa 429
    /// (Too Many Requests) qaytarır, varsa null.
    /// </summary>
    private async Task<IActionResult?> CheckUsageAsync(bool hasImages, CancellationToken cancellationToken)
    {
        var decision = hasImages
            ? await _usageLimitService.CheckOcrAsync(UserId, cancellationToken)
            : await _usageLimitService.CheckTextAsync(UserId, cancellationToken);

        return decision.Allowed
            ? null
            : StatusCode(StatusCodes.Status429TooManyRequests, new { message = decision.Reason });
    }

    /// <summary>
    /// Şagird göndərilibsə onun bu müəllimə aid və silinməmiş olduğunu yoxlayır. Başqasının
    /// şagirdi (və ya mövcud olmayan id) "tapılmadı" kimi rədd edilir — mövcudluq faktı sızmır.
    /// Şagird verilməyibsə heç bir sorğu getmir və heç nə rədd edilmir (şagird seçimi opsionaldır).
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

    /// <summary>
    /// Tarixçə siyahısı (səhifələnmiş, axtarış opsional). page ən azı 1, pageSize 1–100 aralığında.
    /// Öz esseləri və şagird esseləri eyni siyahıdadır; hər sətirdə (varsa) şagirdin adı gəlir.
    /// <paramref name="studentId"/> / <paramref name="groupId"/> ilə daraldıla bilər.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> History(
        [FromQuery] string? search,
        [FromQuery] int? studentId,
        [FromQuery] int? groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var history = await _essayService.GetHistoryAsync(UserId, search, studentId, groupId, page, pageSize, cancellationToken);
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
