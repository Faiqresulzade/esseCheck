using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}
