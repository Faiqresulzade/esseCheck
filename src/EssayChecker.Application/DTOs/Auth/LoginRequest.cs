using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "E-mail ünvanı boş ola bilməz.")]
    [EmailAddress(ErrorMessage = "E-mail ünvanı düzgün deyil.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Şifrə boş ola bilməz.")]
    public string Password { get; set; } = null!;
}
