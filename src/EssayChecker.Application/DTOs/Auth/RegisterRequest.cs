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
}
