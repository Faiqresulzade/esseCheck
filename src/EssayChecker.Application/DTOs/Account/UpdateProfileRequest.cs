using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.DTOs.Account;

public sealed class UpdateProfileRequest
{
    [Required(ErrorMessage = "Ad və soyad boş ola bilməz.")]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;
}
