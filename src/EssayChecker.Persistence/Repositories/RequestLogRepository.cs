using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class RequestLogRepository : IRequestLogRepository
{
    private readonly EssayDbContext _db;

    public RequestLogRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        return await _db.RequestLogs
            .Where(l => l.CreatedAt < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
