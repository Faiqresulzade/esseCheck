using EssayChecker.Application.DTOs.Analytics;
using EssayChecker.Application.DTOs.Interfaces;

namespace EssayChecker.Infrastructure.Services.Analytics;

/// <summary>
/// Hesabatlar mövcud esse nəticələrindən qurulur — əlavə AI çağırışı YOXDUR. "Zəif tərəflər"
/// və "tövsiyələr" hər essenin öz AI rəyindən (TeacherFeedback) götürülüb təkrarlanma sayına
/// görə sıralanır, yenidən yazılmır.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analytics;
    private readonly ITeachingRepository _teaching;

    public AnalyticsService(IAnalyticsRepository analytics, ITeachingRepository teaching)
    {
        _analytics = analytics;
        _teaching = teaching;
    }

    public async Task<OverviewAnalyticsResponse> GetOverviewAsync(
        int teacherId, CancellationToken cancellationToken = default)
    {
        var groups = await _teaching.GetGroupsAsync(teacherId, cancellationToken);
        var rows = await _analytics.GetRowsAsync(teacherId, studentId: null, groupId: null, cancellationToken);
        var feedback = await _analytics.GetRecentFeedbackAsync(
            teacherId, null, null, AnalyticsAggregator.FeedbackEssayWindow, cancellationToken);

        var scores = AnalyticsAggregator.BuildScores(rows);
        var since = DateTime.UtcNow.AddDays(-30);

        var byGroup = rows
            .Where(r => r.GroupId is not null)
            .GroupBy(r => r.GroupId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groupSummaries = groups
            .Select(g =>
            {
                var groupRows = byGroup.TryGetValue(g.Id, out var found) ? found : new List<EssayAnalyticsRow>();
                return new GroupSummary(
                    g.Id,
                    g.Name,
                    g.StudentCount,
                    groupRows.Count,
                    groupRows.Count == 0 ? null : AnalyticsAggregator.BuildScores(groupRows).Total);
            })
            .ToList();

        return new OverviewAnalyticsResponse(
            groups.Count,
            groups.Sum(g => g.StudentCount),
            rows.Count,
            rows.Count(r => r.StudentId is not null),
            rows.Count(r => r.CreatedAt >= since),
            rows.Count >= AnalyticsAggregator.MinEssaysForTrend,
            scores,
            AnalyticsAggregator.WeakestDirection(scores, rows.Count),
            AnalyticsAggregator.BuildMistakes(rows),
            AnalyticsAggregator.BuildHighlights(feedback.SelectMany(f => f.Weaknesses)),
            AnalyticsAggregator.BuildHighlights(feedback.SelectMany(f => f.Recommendations)),
            groupSummaries);
    }

    public async Task<GroupAnalyticsResponse?> GetGroupAsync(
        int teacherId, int groupId, CancellationToken cancellationToken = default)
    {
        // Yad (və ya silinmiş) qrup "tapılmadı" kimi qayıdır — mövcudluq faktı sızmır.
        if (!await _teaching.GroupExistsAsync(teacherId, groupId, cancellationToken))
            return null;

        var group = (await _teaching.GetGroupsAsync(teacherId, cancellationToken))
            .First(g => g.Id == groupId);

        var students = await _teaching.GetStudentsAsync(teacherId, groupId, cancellationToken);
        var rows = await _analytics.GetRowsAsync(teacherId, studentId: null, groupId, cancellationToken);

        var byStudent = rows
            .Where(r => r.StudentId is not null)
            .GroupBy(r => r.StudentId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<EssayAnalyticsRow>)g.ToList());

        var summaries = students
            .Select(s =>
            {
                var studentRows = byStudent.TryGetValue(s.Id, out var found)
                    ? found
                    : Array.Empty<EssayAnalyticsRow>();

                var studentScores = AnalyticsAggregator.BuildScores(studentRows);
                var (latest, _, delta) = AnalyticsAggregator.LatestProgress(studentRows);

                return new GroupStudentSummary(
                    s.Id,
                    s.FullName,
                    0, // sıra aşağıda verilir
                    studentRows.Count,
                    studentRows.Count == 0 ? null : studentScores.Total,
                    latest,
                    delta,
                    AnalyticsAggregator.WeakestDirection(studentScores, studentRows.Count));
            })
            .ToList();

        // Essesi olanlar orta bala görə sıralanır və 1-dən nömrələnir; essesi olmayanlar
        // sonda, Rank = 0 (frontend onları "hələ esse yoxdur" kimi göstərir).
        var ranked = summaries
            .Where(s => s.EssayCount > 0)
            .OrderByDescending(s => s.AverageTotal)
            .ThenBy(s => s.FullName, StringComparer.CurrentCultureIgnoreCase)
            .Select((s, index) => s with { Rank = index + 1 })
            .Concat(summaries
                .Where(s => s.EssayCount == 0)
                .OrderBy(s => s.FullName, StringComparer.CurrentCultureIgnoreCase))
            .ToList();

        var scores = AnalyticsAggregator.BuildScores(rows);

        return new GroupAnalyticsResponse(
            group.Id,
            group.Name,
            group.StudentCount,
            rows.Count,
            rows.Count >= AnalyticsAggregator.MinEssaysForTrend,
            scores,
            AnalyticsAggregator.WeakestDirection(scores, rows.Count),
            AnalyticsAggregator.BuildMistakes(rows),
            ranked);
    }

    public async Task<StudentAnalyticsResponse?> GetStudentAsync(
        int teacherId, int studentId, CancellationToken cancellationToken = default)
    {
        var student = await _teaching.GetStudentAsync(teacherId, studentId, cancellationToken);
        if (student is null)
            return null;

        var rows = await _analytics.GetRowsAsync(teacherId, studentId, groupId: null, cancellationToken);
        var feedback = await _analytics.GetRecentFeedbackAsync(
            teacherId, studentId, null, AnalyticsAggregator.FeedbackEssayWindow, cancellationToken);

        var scores = AnalyticsAggregator.BuildScores(rows);
        var (latest, previous, delta) = AnalyticsAggregator.LatestProgress(rows);

        return new StudentAnalyticsResponse(
            student.Id,
            student.FullName,
            student.GroupId,
            student.GroupName,
            student.Grade,
            rows.Count,
            rows.Count >= AnalyticsAggregator.MinEssaysForTrend,
            scores,
            AnalyticsAggregator.WeakestDirection(scores, rows.Count),
            latest,
            previous,
            delta,
            AnalyticsAggregator.BuildMistakes(rows),
            AnalyticsAggregator.BuildTrend(rows),
            AnalyticsAggregator.BuildHighlights(feedback.SelectMany(f => f.Weaknesses)),
            AnalyticsAggregator.BuildHighlights(feedback.SelectMany(f => f.Recommendations)));
    }
}
