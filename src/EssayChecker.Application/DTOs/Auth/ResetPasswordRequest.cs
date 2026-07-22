using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "E-mail ünvanı boş ola bilməz.")]
    [EmailAddress(ErrorMessage = "E-mail ünvanı düzgün deyil.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Token boş ola bilməz.")]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "Yeni şifrə boş ola bilməz.")]
    [MinLength(8, ErrorMessage = "Şifrə ən azı 8 simvoldan ibarət olmalıdır.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Şifrə təsdiqi boş ola bilməz.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Şifrələr uyğun gəlmir.")]
    public string ConfirmPassword { get; set; } = null!;
}
