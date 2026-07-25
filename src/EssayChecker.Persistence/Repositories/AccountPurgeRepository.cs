using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class AccountPurgeRepository : IAccountPurgeRepository
{
    private readonly EssayDbContext _db;

    public AccountPurgeRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task<int> PurgeExpiredDeletedAccountsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        // FK-lar Cascade olduğu üçün (Essays, UserSubscriptions, DailyUsages, RefreshTokens,
        // Identity-nin öz Claims/Logins/Roles/Tokens cədvəlləri) istifadəçi sətri silinəndə
        // bağlı bütün məlumatlar bazanın özü tərəfindən avtomatik silinir.
        return await _db.Users
            .Where(u => u.IsDeleted && u.DeletedAt != null && u.DeletedAt <= cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
