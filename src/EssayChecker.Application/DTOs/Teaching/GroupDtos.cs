using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.DTOs.Teaching;

/// <summary>Qrup siyahısı elementi — şagird sayı ilə birlikdə ("11-A · 14 şagird").</summary>
public sealed record GroupResponse(
    int Id,
    string Name,
    int StudentCount,
    DateTime CreatedAt);

public sealed class SaveGroupRequest
{
    [Required(ErrorMessage = "Qrup adı boş ola bilməz.")]
    [MaxLength(100, ErrorMessage = "Qrup adı maksimum 100 simvol ola bilər.")]
    public string Name { get; set; } = null!;
}
