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

    /// <summary>
    /// Play Store-da mövcud olan ən son versiya (məs. "1.3.0"). Yeni versiya çıxanda bu dəyər
    /// Render-də env var (App__LatestVersion) ilə dəyişdirilir — kod dəyişikliyi/redeploy lazım
    /// deyil. Boş qalarsa /api/App/version-check həmişə "yeniləmə yoxdur" qaytarır.
    /// </summary>
    public string? LatestVersion { get; set; }

    /// <summary>Tətbiqin Play Store səhifəsinin tam linki — istifadəçi "Yenilə" düyməsinə basanda açılır.</summary>
    public string? PlayStoreUrl { get; set; }

    /// <summary>
    /// Referal (dəvət et, endirim qazan) proqramının açarı. Qəsdən defolt false-dur — kod tam
    /// hazırdır, amma proqram rəsmən başlayana qədər deaktivdir. Lazım olanda Render-də
    /// App__ReferralProgramEnabled=true edərək, redeploy tələb etmədən aktivləşdirilir.
    /// </summary>
    public bool ReferralProgramEnabled { get; set; } = false;
}
