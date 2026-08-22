using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Domain.Entities.Lessons;
using EssayChecker.Domain.Enums;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class LessonRepository : ILessonRepository
{
    private readonly EssayDbContext _db;

    public LessonRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Lesson?> GetByIdAsync(int lessonId, CancellationToken cancellationToken = default) =>
        _db.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

    public Task<Lesson?> FindByTopicAsync(
        string normalizedTopic, GradeLevel grade, CancellationToken cancellationToken = default) =>
        _db.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.NormalizedTopic == normalizedTopic && l.Grade == grade, cancellationToken);

    public async Task<LessonHistoryResponse> GetLibraryAsync(
        int currentUserId, string? search, GradeLevel? grade, bool onlyMine, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Lessons.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.Topic.Contains(search));

        if (grade is not null)
            query = query.Where(l => l.Grade == grade);

        if (onlyMine)
            query = query.Where(l => l.CreatedByUserId == currentUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LessonListItemResponse(
                l.Id,
                l.Topic,
                l.Grade,
                _db.Users
                    .Where(u => u.Id == l.CreatedByUserId)
                    .Select(u => u.FullName)
                    .FirstOrDefault() ?? string.Empty,
                l.CreatedByUserId == currentUserId,
                l.Slides.Count,
                l.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new LessonHistoryResponse(items, totalCount, page, pageSize, totalPages);
    }

    public Task<string?> GetCreatorNameAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);
}
