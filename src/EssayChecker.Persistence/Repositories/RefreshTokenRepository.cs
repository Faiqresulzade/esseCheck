using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Users;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly EssayDbContext _db;

    public RefreshTokenRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.RevokedAt, utcNow), cancellationToken);
    }

    public async Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await _db.RefreshTokens
            .Where(t => t.ExpiresAt < utcNow)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
