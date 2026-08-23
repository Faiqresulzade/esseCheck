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

    public async Task<bool> TryClaimAsync(DeviceTrial trial, CancellationToken cancellationToken = default)
    {
        // Əvvəlcə sadə yoxlama (adi hal), sonra unikal indeks yarışı üçün ehtiyat.
        var alreadyUsed = await _db.DeviceTrials
            .AsNoTracking()
            .AnyAsync(d => d.DeviceIdHash == trial.DeviceIdHash, cancellationToken);

        if (alreadyUsed)
            return false;

        _db.DeviceTrials.Add(trial);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Eyni cihazdan iki qeydiyyat eyni anda gəldi — unikal indeks ikincisini rədd etdi.
            // Bu, xəta deyil: sadəcə bu sorğu trial almır.
            _db.Entry(trial).State = EntityState.Detached;
            return false;
        }
    }
}
