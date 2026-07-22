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
}
