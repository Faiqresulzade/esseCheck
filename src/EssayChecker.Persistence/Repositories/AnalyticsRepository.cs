using EssayChecker.Application.DTOs.Analytics;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Essays;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class AnalyticsRepository : IAnalyticsRepository
{
    private readonly EssayDbContext _db;

    public AnalyticsRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EssayAnalyticsRow>> GetRowsAsync(
        int teacherId, int? studentId, int? groupId, CancellationToken cancellationToken = default)
    {
        return await Filtered(teacherId, studentId, groupId)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new EssayAnalyticsRow(
                e.Id,
                e.StudentId,
                _db.Students
                    .Where(s => s.Id == e.StudentId && !s.IsDeleted)
                    .Select(s => (int?)s.GroupId)
                    .FirstOrDefault(),
                e.CreatedAt,
                e.Title,
                e.WordCount,
                e.Scores.Total,
                e.Scores.Structure,
                e.Scores.Content,
                e.Scores.Grammar,
                e.Scores.Vocabulary,
                e.Statistics.Total,
                e.Statistics.Grammar,
                e.Statistics.Spelling,
                e.Statistics.Vocabulary,
                e.Statistics.NaturalExpression))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeedbackRow>> GetRecentFeedbackAsync(
        int teacherId, int? studentId, int? groupId, int take, CancellationToken cancellationToken = default)
    {
        var feedbacks = await Filtered(teacherId, studentId, groupId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .Select(e => e.Feedback)
            .ToListAsync(cancellationToken);

        return feedbacks
            .Select(f => new FeedbackRow(
                f.Weaknesses ?? new List<string>(),
                f.Recommendations ?? new List<string>()))
            .ToList();
    }

    /// <summary>
    /// Müəllimin esseləri + opsional şagird/qrup filtri. Qrup filtrində silinmiş şagirdlər
    /// çıxarılır ki, hesabatdakı rəqəmlər ekranda görünən şagird siyahısı ilə uyğun gəlsin
    /// (tarixçə filtrindən fərqli olaraq — orada silinmiş şagirdin essesi də tapılmalıdır).
    /// </summary>
    private IQueryable<Essay> Filtered(int teacherId, int? studentId, int? groupId)
    {
        var query = _db.Essays
            .AsNoTracking()
            .Where(e => e.UserId == teacherId);

        if (studentId is not null)
            query = query.Where(e => e.StudentId == studentId);

        if (groupId is not null)
        {
            query = query.Where(e => _db.Students.Any(s =>
                s.Id == e.StudentId
                && s.GroupId == groupId
                && !s.IsDeleted
                && s.Group.TeacherId == teacherId));
        }

        return query;
    }
}
