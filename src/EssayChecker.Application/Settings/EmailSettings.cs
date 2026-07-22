using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.Settings;

public sealed class EmailSettings
{
    public const string SectionName = "Email";

    [Required]
    public string Host { get; set; } = "smtp.gmail.com";

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    [Required]
    public string SenderName { get; set; } = "EssayCheck AI";

    [Required]
    [EmailAddress]
    public string SenderEmail { get; set; } = null!;

    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    public bool EnableSsl { get; set; } = true;
}
