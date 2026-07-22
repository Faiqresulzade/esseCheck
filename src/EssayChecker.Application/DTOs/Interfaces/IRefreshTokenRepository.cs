using EssayChecker.Domain.Entities.Users;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>İstifadəçinin bütün aktiv refresh token-lərini revoke edir (çıxış / şifrə dəyişmə / hesab silmə).</summary>
    Task RevokeAllAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>Vaxtı artıq bitmiş (istifadə oluna bilməyəcək) token-ləri silir. Silinən sayı qaytarılır.</summary>
    Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
