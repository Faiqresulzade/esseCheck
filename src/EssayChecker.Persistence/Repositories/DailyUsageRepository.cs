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
        IncrementAsync(userId, usageDate, Counter.Text, cancellationToken);

    public Task IncrementOcrAsync(int userId, DateOnly usageDate, CancellationToken cancellationToken = default) =>
        IncrementAsync(userId, usageDate, Counter.Ocr, cancellationToken);

    public Task IncrementLessonAsync(int userId, DateOnly usageDate, CancellationToken cancellationToken = default) =>
        IncrementAsync(userId, usageDate, Counter.Lesson, cancellationToken);

    private enum Counter { Text, Ocr, Lesson }

    private async Task IncrementAsync(int userId, DateOnly usageDate, Counter counter, CancellationToken cancellationToken)
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
                TextCheckCount = counter == Counter.Text ? 1 : 0,
                OcrCheckCount = counter == Counter.Ocr ? 1 : 0,
                LessonCount = counter == Counter.Lesson ? 1 : 0,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            switch (counter)
            {
                case Counter.Text: row.TextCheckCount++; break;
                case Counter.Ocr: row.OcrCheckCount++; break;
                default: row.LessonCount++; break;
            }

            row.UpdatedAt = now;
            _db.DailyUsages.Update(row);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
