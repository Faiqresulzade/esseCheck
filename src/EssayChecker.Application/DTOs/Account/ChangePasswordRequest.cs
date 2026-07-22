using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.DTOs.Account;

public sealed class ChangePasswordRequest
{
    [Required(ErrorMessage = "Cari şifrə boş ola bilməz.")]
    public string CurrentPassword { get; set; } = null!;

    [Required(ErrorMessage = "Yeni şifrə boş ola bilməz.")]
    [MinLength(8, ErrorMessage = "Şifrə ən azı 8 simvoldan ibarət olmalıdır.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Şifrə təsdiqi boş ola bilməz.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Şifrələr uyğun gəlmir.")]
    public string ConfirmPassword { get; set; } = null!;
}
