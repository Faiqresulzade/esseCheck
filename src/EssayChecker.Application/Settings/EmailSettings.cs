using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.Settings;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    [Required]
    public string Host { get; set; } = "smtp.gmail.com";

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    [Required]
    public string SenderName { get; set; } = "EssayCheck AI";

    [Required]
    [EmailAddress]
    public string SenderEmail { get; set; } = null!;

    /// <summary>Yalnız SMTP rejimi üçün lazımdır (Brevo istifadə olunanda boş qala bilər).</summary>
    public string? Username { get; set; }

    /// <summary>Yalnız SMTP rejimi üçün lazımdır (Brevo istifadə olunanda boş qala bilər).</summary>
    public string? Password { get; set; }

    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Brevo (HTTP API) açarı. Doldurulubsa e-mail SMTP əvəzinə Brevo-nun HTTPS API-si ilə
    /// göndərilir. Bu vacibdir, çünki Render kimi hosting platformaları çıxan SMTP portlarını
    /// (25/465/587) bloklayır — HTTPS (443) isə heç vaxt bloklanmır.
    /// Boş qalarsa sistem avtomatik SMTP rejimində işləyir (lokal development üçün əlverişlidir).
    /// </summary>
    public string? BrevoApiKey { get; set; }

    [Url]
    public string BrevoBaseUrl { get; set; } = "https://api.brevo.com/v3/smtp/email";
}
