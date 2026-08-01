using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.Settings;

public sealed class OpenRouterSettings
{
    public const string SectionName = "OpenRouter";

    [Required]
    public string ApiKey { get; set; } = null!;

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";

    /// <summary>Esse qiymətləndirmə üçün model (OpenRouter model id).</summary>
    [Required]
    public string Model { get; set; } = null!;

    /// <summary>Şəkildən mətn oxumaq (OCR) üçün vision model.</summary>
    [Required]
    public string OcrModel { get; set; } = null!;

    /// <summary>
    /// Pulsuz model (Model) uğursuz olduqda (JSON xətası/keçici xəta) müraciət olunacaq pullu,
    /// ehtiyat model. Opsionaldır — boş qalarsa fallback aktivləşmir. Qəsdən [Required] deyil ki,
    /// bu doldurulmayanda tətbiqin qalan hissəsi bloklanmasın (GooglePlaySettings ilə eyni prinsip).
    /// </summary>
    public string? FallbackModel { get; set; }

    /// <summary>
    /// 0 = maksimum determinizm. Qiymətləndirmə subyektiv "yaradıcılıq" deyil, sabit rubrikaya
    /// əsaslanan ölçmədir — eyni esse hər dəfə eyni nəticəni verməlidir.
    /// </summary>
    public float Temperature { get; set; } = 0f;

    /// <summary>
    /// Çıxış token limiti. Yüksək dəyər generasiya vaxtını artırır (autoregressive modellərdə
    /// cavab vaxtı çıxış token sayı ilə düz mütənasibdir) — 5000 simvollu maksimum esse üçün
    /// 4096 kifayət qədər genişdir, artırma performansı əhəmiyyətli dərəcədə pisləşdirir.
    /// </summary>
    [Range(1, 100000)]
    public int MaxTokens { get; set; } = 4096;

    /// <summary>OpenRouter reytinqləri üçün opsional başlıqlar.</summary>
    public string? Referer { get; set; }

    public string? Title { get; set; } = "EssayCheck AI";
}
