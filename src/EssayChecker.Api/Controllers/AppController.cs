using EssayChecker.Application.DTOs.App;
using EssayChecker.Application.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EssayChecker.Api.Controllers;

/// <summary>Tətbiqin özü ilə bağlı (istifadəçidən asılı olmayan) məlumatlar — hazırda versiya yoxlanışı.</summary>
[AllowAnonymous]
[Route("api/[controller]")]
[ApiController]
public class AppController : ControllerBase
{
    private readonly AppSettings _settings;

    public AppController(IOptions<AppSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>
    /// Tətbiq açılanda çağırılır: cari versiyanı göndərir, Play Store-dakı son versiyadan
    /// köhnədirsə "yeniləmə var" bildirişi üçün lazım olan hər şeyi (link daxil) qaytarır.
    /// Bu, yalnız OPSIONAL bildirişdir — məcburi yeniləmə/bloklama yoxdur.
    /// </summary>
    [HttpGet("version-check")]
    public IActionResult VersionCheck([FromQuery] string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
            return BadRequest(new { message = "currentVersion parametri tələb olunur." });

        if (string.IsNullOrWhiteSpace(_settings.LatestVersion))
            return Ok(new VersionCheckResponse(false, _settings.LatestVersion, _settings.PlayStoreUrl));

        // Versiyalar sətir kimi deyil, Version tipi ilə müqayisə olunur ki, "1.10.0" səhvən
        // "1.9.0"-dan kiçik sayılmasın. Hər hansı biri parse olunmasa, təhlükəsiz defolt olaraq
        // "yeniləmə yoxdur" qaytarılır — yanlış xəbərdarlıqdan yayınmaq üçün.
        var updateAvailable =
            Version.TryParse(currentVersion, out var current) &&
            Version.TryParse(_settings.LatestVersion, out var latest) &&
            latest > current;

        return Ok(new VersionCheckResponse(updateAvailable, _settings.LatestVersion, _settings.PlayStoreUrl));
    }
}
