namespace EssayChecker.Domain.Entities.Users;

/// <summary>
/// Refresh token (yalnız SHA-256 hash-i saxlanılır — DB sızıntısında token istifadə olunmasın).
/// Rotasiya: hər yeniləmədə köhnə revoke olunur, yenisi yaradılır.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;
}
