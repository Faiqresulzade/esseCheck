using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class DeviceTrialRepository : IDeviceTrialRepository
{
    private readonly EssayDbContext _db;

    public DeviceTrialRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryClaimAndGrantAsync(
        DeviceTrial trial, UserSubscription subscription, CancellationToken cancellationToken = default)
    {
        // Əvvəlcə sadə yoxlama (adi hal), sonra unikal indeks yarışı üçün ehtiyat.
        var alreadyUsed = await _db.DeviceTrials
            .AsNoTracking()
            .AnyAsync(d => d.DeviceIdHash == trial.DeviceIdHash, cancellationToken);

        if (alreadyUsed)
            return false;

        // Cihaz qeydi və abunəlik bir yerdə ya yazılır, ya da heç biri yazılmır.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.DeviceTrials.Add(trial);
        _db.UserSubscriptions.Add(subscription);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Eyni cihazdan iki qeydiyyat eyni anda gəldi — unikal indeks ikincisini rədd etdi.
            // Bu, xəta deyil: sadəcə bu sorğu trial almır.
            await transaction.RollbackAsync(cancellationToken);
            _db.Entry(trial).State = EntityState.Detached;
            _db.Entry(subscription).State = EntityState.Detached;
            return false;
        }
    }
}
