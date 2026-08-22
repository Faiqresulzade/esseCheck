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

    public Task<Lesson?> GetByIdAsync(int userId, int lessonId, CancellationToken cancellationToken = default) =>
        _db.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.UserId == userId, cancellationToken);

    public Task<Lesson?> FindOwnAsync(
        int userId, string normalizedTopic, GradeLevel grade, CancellationToken cancellationToken = default) =>
        _db.Lessons
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.NormalizedTopic == normalizedTopic && l.Grade == grade)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<LessonHistoryResponse> GetHistoryAsync(
        int userId, string? search, int? studentId, int? groupId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Lessons
            .AsNoTracking()
            .Where(l => l.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.Topic.Contains(search));

        if (studentId is not null)
            query = query.Where(l => l.StudentId == studentId);

        // Qrup filtri: silinmiş şagirdlər də daxildir — esse tarixçəsindəki eyni davranış.
        if (groupId is not null)
        {
            query = query.Where(l => _db.Students
                .Any(s => s.Id == l.StudentId && s.GroupId == groupId && s.Group.TeacherId == userId));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LessonListItemResponse(
                l.Id,
                l.Topic,
                l.Grade,
                l.StudentId,
                _db.Students
                    .Where(s => s.Id == l.StudentId)
                    .Select(s => s.FullName)
                    .FirstOrDefault(),
                l.Slides.Count,
                l.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new LessonHistoryResponse(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<bool> DeleteAsync(int userId, int lessonId, CancellationToken cancellationToken = default)
    {
        var affected = await _db.Lessons
            .Where(l => l.Id == lessonId && l.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return affected > 0;
    }

    public Task<LessonTemplate?> FindTemplateAsync(
        string normalizedTopic, GradeLevel grade, int promptVersion, CancellationToken cancellationToken = default) =>
        _db.LessonTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.NormalizedTopic == normalizedTopic && t.Grade == grade && t.PromptVersion == promptVersion,
                cancellationToken);

    public async Task AddTemplateAsync(LessonTemplate template, CancellationToken cancellationToken = default)
    {
        _db.LessonTemplates.Add(template);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // İki istifadəçi eyni mövzunu eyni anda soruşsa unikal indeks pozula bilər. Keşin
            // yazıla bilməməsi istifadəçi üçün xəta deyil — dərsi onsuz da almış olur.
            _db.Entry(template).State = EntityState.Detached;
        }
    }
}
