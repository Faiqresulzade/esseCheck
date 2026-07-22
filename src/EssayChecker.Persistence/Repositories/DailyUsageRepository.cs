using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class DailyUsageRepository : IDailyUsageRepository
{
    private readonly EssayDbContext _db;

    public DailyUsageRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task<DailyUsage?> GetAsync(int userId, DateOnly usageDate, CancellationToken cancellationToken = default)
    {
        return await _db.DailyUsages
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.UsageDate == usageDate, cancellationToken);
    }

    public Task IncrementTextAsync(int userId, DateOnly usageDate, CancellationToken cancellationToken = default) =>
        IncrementAsync(userId, usageDate, isText: true, cancellationToken);

    public Task IncrementOcrAsync(int userId, DateOnly usageDate, CancellationToken cancellationToken = default) =>
        IncrementAsync(userId, usageDate, isText: false, cancellationToken);

    private async Task IncrementAsync(int userId, DateOnly usageDate, bool isText, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var row = await _db.DailyUsages
            .FirstOrDefaultAsync(u => u.UserId == userId && u.UsageDate == usageDate, cancellationToken);

        if (row is null)
        {
            _db.DailyUsages.Add(new DailyUsage
            {
                UserId = userId,
                UsageDate = usageDate,
                TextCheckCount = isText ? 1 : 0,
                OcrCheckCount = isText ? 0 : 1,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            if (isText)
                row.TextCheckCount++;
            else
                row.OcrCheckCount++;

            row.UpdatedAt = now;
            _db.DailyUsages.Update(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
