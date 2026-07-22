using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Essays;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class EssayRepository : IEssayRepository
{
    private readonly EssayDbContext _db;

    public EssayRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Essay essay, CancellationToken cancellationToken = default)
    {
        _db.Essays.Add(essay);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Essay?> GetByIdAsync(int userId, int essayId, CancellationToken cancellationToken = default)
    {
        return await _db.Essays
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == essayId && e.UserId == userId, cancellationToken);
    }

    public async Task<EssayHistoryResponse> GetHistoryAsync(
        int userId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Essays
            .AsNoTracking()
            .Where(e => e.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Title.Contains(search));

        // Say və ortalama filtrlənmiş TAM dəstə üzərindən hesablanır, Items isə yalnız cari səhifə.
        var totalCount = await query.CountAsync(cancellationToken);
        var averageScore = totalCount > 0
            ? Math.Round(await query.AverageAsync(e => e.TotalScore, cancellationToken), 1)
            : 0;

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EssayListItemResponse(
                e.Id,
                e.Title,
                e.CreatedAt,
                e.WordCount,
                e.TotalScore))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new EssayHistoryResponse(items, totalCount, averageScore, page, pageSize, totalPages);
    }

    public async Task<bool> DeleteAsync(int userId, int essayId, CancellationToken cancellationToken = default)
    {
        var affected = await _db.Essays
            .Where(e => e.Id == essayId && e.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return affected > 0;
    }

    public async Task<int> DeleteAllAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.Essays
            .Where(e => e.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
