using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class ProcessedNotificationRepository : IProcessedNotificationRepository
{
    private readonly EssayDbContext _db;

    public ProcessedNotificationRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryMarkProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var alreadyProcessed = await _db.ProcessedGoogleNotifications
            .AsNoTracking()
            .AnyAsync(n => n.MessageId == messageId, cancellationToken);

        if (alreadyProcessed)
            return false;

        _db.ProcessedGoogleNotifications.Add(new ProcessedGoogleNotification
        {
            MessageId = messageId,
            ProcessedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // Unique index toqquşması — paralel çağırışda başqa instans artıq işarələdi.
            return false;
        }
    }
}
