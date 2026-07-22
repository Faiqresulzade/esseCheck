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

    public float Temperature { get; set; } = 0.2f;

    [Range(1, 100000)]
    public int MaxTokens { get; set; } = 10000;

    /// <summary>OpenRouter reytinqləri üçün opsional başlıqlar.</summary>
    public string? Referer { get; set; }

    public string? Title { get; set; } = "EssayCheck AI";
}
