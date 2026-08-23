using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Ad və soyad boş ola bilməz.")]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "E-mail ünvanı boş ola bilməz.")]
    [EmailAddress(ErrorMessage = "E-mail ünvanı düzgün deyil.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Şifrə boş ola bilməz.")]
    [MinLength(8, ErrorMessage = "Şifrə ən azı 8 simvoldan ibarət olmalıdır.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Şifrə təsdiqi boş ola bilməz.")]
    [Compare(nameof(Password), ErrorMessage = "Şifrələr uyğun gəlmir.")]
    public string ConfirmPassword { get; set; } = null!;

    /// <summary>İstifadə şərtləri və Gizlilik siyasətinin qəbul edilməsi.</summary>
    public bool AcceptTerms { get; set; }

    /// <summary>
    /// Opsional — başqa istifadəçinin dəvət linkindən gələn kod. Etibarsız/naməlum koddursa
    /// sükutla nəzərə alınmır, qeydiyyatı bloklamır.
    /// </summary>
    [MaxLength(10)]
    public string? ReferralCode { get; set; }

    /// <summary>
    /// Cihaz identifikatoru (Android: Settings.Secure.ANDROID_ID). Pulsuz 1 aylıq sınağın
    /// yalnız BİR dəfə verilməsi üçündür — bax DeviceTrial.
    ///
    /// Opsionaldır: göndərilməsə qeydiyyat normal davam edir, sadəcə trial VERİLMİR (istifadəçi
    /// Free planda qalır). Bu, qəsdəndir — əks halda başlığı göndərməməklə qorumanı keçmək olardı.
    /// </summary>
    [MaxLength(200)]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Google Play Integrity token-i. Hazırda yoxlanmır (Play Integrity qurulmayıb), amma sahə
    /// indidən qəbul olunur ki, mobil tərəf bir dəfə göndərməyə başlasın və server tərəfdə
    /// yoxlama aktivləşəndə tətbiqin yeni versiyası tələb olunmasın.
    /// </summary>
    public string? IntegrityToken { get; set; }
}
