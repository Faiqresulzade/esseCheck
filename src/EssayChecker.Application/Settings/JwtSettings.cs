using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32, ErrorMessage = "Jwt:Key ən azı 32 simvol olmalıdır (HS256 üçün).")]
    public string Key { get; set; } = null!;

    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int ExpiryMinutes { get; set; } = 120;

    [Range(1, 365)]
    public int RefreshTokenDays { get; set; } = 30;
}
