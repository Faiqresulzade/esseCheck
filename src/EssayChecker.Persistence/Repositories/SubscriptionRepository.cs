using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Subscriptions;
using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly EssayDbContext _db;

    public SubscriptionRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task<UserSubscription?> GetActiveAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserSubscription?> GetByPurchaseTokenAsync(string purchaseToken, CancellationToken cancellationToken = default)
    {
        var hash = PurchaseTokenHasher.Hash(purchaseToken);

        return await _db.UserSubscriptions
            .AsNoTracking()
            .Where(s => s.PurchaseTokenHash == hash)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default)
    {
        _db.UserSubscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserSubscription subscription, CancellationToken cancellationToken = default)
    {
        _db.UserSubscriptions.Update(subscription);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAllAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await _db.UserSubscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.IsActive, false)
                .SetProperty(s => s.UpdatedAt, utcNow), cancellationToken);
    }

    public async Task DeactivateOthersAsync(int userId, int keepSubscriptionId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await _db.UserSubscriptions
            .Where(s => s.UserId == userId && s.IsActive && s.Id != keepSubscriptionId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.IsActive, false)
                .SetProperty(s => s.UpdatedAt, utcNow), cancellationToken);
    }
}
