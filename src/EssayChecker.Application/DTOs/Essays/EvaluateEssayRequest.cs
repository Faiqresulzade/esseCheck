using System.ComponentModel.DataAnnotations;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Essays;

public sealed class EvaluateEssayRequest
{
    [Required(ErrorMessage = "Esse mətni boş ola bilməz.")]
    [MinLength(1)]
    [MaxLength(5000, ErrorMessage = "Esse maksimum 5000 simvol ola bilər.")]
    public string Text { get; set; } = null!;

    /// <summary>Opsional başlıq; boş olsa mətnin əvvəlindən avtomatik yaradılır.</summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>Mətnin mənbəyi: birbaşa yazılıb (Text) yoxsa şəkildən oxunub (Image).</summary>
    public EssayInputSource Source { get; set; } = EssayInputSource.Text;

    /// <summary>
    /// Hansı sinif üçün qiymətləndirilir (9 və ya 11) — DİM meyarları (minimum söz sayı və s.)
    /// sinifə görə fərqləndiyi üçün AI-a göndərilən promt bu dəyərə görə seçilir.
    /// </summary>
    [Required(ErrorMessage = "Sinif seçilməlidir.")]
    [EnumDataType(typeof(GradeLevel), ErrorMessage = "Sinif dəyəri etibarsızdır.")]
    public GradeLevel Grade { get; set; }

    /// <summary>
    /// Opsional tapşırıq mövzusu (məs. "Should students wear school uniforms?"). Verilibsə,
    /// AI content balını essenin özündən çıxardığı mövzu əvəzinə birbaşa bu mövzuya görə
    /// qiymətləndirir. Boş qalarsa AI mövzunu essenin özündən çıxarır.
    /// </summary>
    [MaxLength(300)]
    public string? Topic { get; set; }
}
