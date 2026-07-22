using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.Settings;

public sealed class AppSettings
{
    public const string SectionName = "App";

    /// <summary>
    /// Frontend-də şifrə sıfırlama səhifəsinin ünvanı.
    /// E-mail-də göndərilən link bu ünvanın üzərinə token və email əlavə edilərək qurulur.
    /// </summary>
    [Required]
    [Url]
    public string ResetPasswordUrl { get; set; } = null!;
}
