using EssayChecker.Application.DTOs.Admin;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Enums;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class AdminReportRepository : IAdminReportRepository
{
    private readonly EssayDbContext _db;

    public AdminReportRepository(EssayDbContext db)
    {
        _db = db;
    }

    public async Task<AdminOverviewResponse> GetOverviewAsync(
        AdminPeriod period, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // --- İstifadəçilər ---
        var totalUsers = await _db.Users.CountAsync(cancellationToken);
        var newUsers = await _db.Users.CountAsync(u => u.CreatedAt >= fromUtc && u.CreatedAt < toUtc, cancellationToken);
        var softDeleted = await _db.Users.CountAsync(u => u.IsDeleted, cancellationToken);
        var activeUsers = await _db.Essays
            .Where(e => e.CreatedAt >= fromUtc && e.CreatedAt < toUtc)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        // --- Abunəliklər ---
        // Aktiv = IsActive və müddəti bitməyib. Testing:ForceProPlusForAllUsers bayrağı QƏSDƏN
        // nəzərə alınmır: sahibkara bazadakı həqiqi mənzərə lazımdır, effektiv hüquqlar yox.
        var activeSubs = await _db.UserSubscriptions
            .Where(s => s.IsActive && (s.EndDate == null || s.EndDate > now))
            .Select(s => new { s.Plan, s.Platform })
            .ToListAsync(cancellationToken);

        var realPurchasesInPeriod = await _db.UserSubscriptions
            .CountAsync(s => s.Platform == SubscriptionPlatform.GooglePlay
                             && s.CreatedAt >= fromUtc && s.CreatedAt < toUtc, cancellationToken);

        var subscriptionStats = new AdminSubscriptionStats(
            ActiveTotal: activeSubs.Count,
            RealPurchasesActive: activeSubs.Count(s => s.Platform == SubscriptionPlatform.GooglePlay),
            RealPurchasesInPeriod: realPurchasesInPeriod,
            TrialsActive: activeSubs.Count(s => s.Platform == SubscriptionPlatform.Trial),
            ByPlan: activeSubs.GroupBy(s => s.Plan)
                .Select(g => new AdminPlanCount(g.Key, g.Count()))
                .OrderByDescending(x => x.Count).ToList(),
            ByPlatform: activeSubs.GroupBy(s => s.Platform)
                .Select(g => new AdminPlatformCount(g.Key, g.Count()))
                .OrderByDescending(x => x.Count).ToList());

        // --- Esselər ---
        var totalEssays = await _db.Essays.CountAsync(cancellationToken);
        var essaysInPeriod = await _db.Essays
            .CountAsync(e => e.CreatedAt >= fromUtc && e.CreatedAt < toUtc, cancellationToken);

        var byGrade = await _db.Essays
            .Where(e => e.CreatedAt >= fromUtc && e.CreatedAt < toUtc)
            .GroupBy(e => e.Grade)
            .Select(g => new { Grade = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var bySource = await _db.Essays
            .Where(e => e.CreatedAt >= fromUtc && e.CreatedAt < toUtc)
            .GroupBy(e => e.InputSource)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var essayStats = new AdminEssayStats(
            totalEssays,
            essaysInPeriod,
            byGrade.Select(g => new AdminNamedCount(g.Grade.ToString(), g.Count)).ToList(),
            bySource.Select(g => new AdminNamedCount(g.Source.ToString(), g.Count)).ToList());

        // --- Məzmun ---
        var contentStats = new AdminContentStats(
            LessonsTotal: await _db.Lessons.CountAsync(cancellationToken),
            LessonsInPeriod: await _db.Lessons.CountAsync(l => l.CreatedAt >= fromUtc && l.CreatedAt < toUtc, cancellationToken),
            ActiveGroups: await _db.StudentGroups.CountAsync(g => !g.IsDeleted, cancellationToken),
            ActiveStudents: await _db.Students.CountAsync(s => !s.IsDeleted, cancellationToken),
            DeviceTrialsUsed: await _db.DeviceTrials.CountAsync(cancellationToken));

        // --- Server sağlamlığı ---
        var healthStats = new AdminHealthStats(
            RequestsInPeriod: await _db.RequestLogs.CountAsync(l => l.CreatedAt >= fromUtc && l.CreatedAt < toUtc, cancellationToken),
            ServerErrorsInPeriod: await _db.RequestLogs.CountAsync(l => l.CreatedAt >= fromUtc && l.CreatedAt < toUtc && l.StatusCode >= 500, cancellationToken),
            RateLimitedInPeriod: await _db.RequestLogs.CountAsync(l => l.CreatedAt >= fromUtc && l.CreatedAt < toUtc && l.StatusCode == 429, cancellationToken));

        return new AdminOverviewResponse(
            period,
            fromUtc == DateTime.MinValue ? DateTime.MinValue : fromUtc,
            toUtc,
            new AdminUserStats(totalUsers, newUsers, activeUsers, softDeleted),
            subscriptionStats,
            essayStats,
            contentStats,
            healthStats);
    }

    public async Task<AdminUserListResponse> GetUsersAsync(
        DateTime? registeredFromUtc, DateTime? registeredToUtc, string? search, bool sortByEssays,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _db.Users.AsNoTracking();

        if (registeredFromUtc is not null)
            query = query.Where(u => u.CreatedAt >= registeredFromUtc);

        if (registeredToUtc is not null)
            query = query.Where(u => u.CreatedAt < registeredToUtc);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || (u.Email != null && u.Email.Contains(search)));

        var totalCount = await query.CountAsync(cancellationToken);

        // Sətir başına alt-sorğular: istifadəçi sayı yüzlərlə ölçüdə olduğu üçün bu, qəbul
        // ediləndir; minlərə çatanda burada JOIN + GroupBy-a keçmək lazım gələcək.
        var projected = query.Select(u => new
        {
            u.Id,
            u.FullName,
            u.Email,
            u.CreatedAt,
            u.LastLoginDate,
            u.IsDeleted,
            EssayCount = _db.Essays.Count(e => e.UserId == u.Id),
            LastEssayAt = _db.Essays.Where(e => e.UserId == u.Id)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => (DateTime?)e.CreatedAt)
                .FirstOrDefault(),
            LessonCount = _db.Lessons.Count(l => l.CreatedByUserId == u.Id),
            GroupCount = _db.StudentGroups.Count(g => g.TeacherId == u.Id && !g.IsDeleted),
            StudentCount = _db.Students.Count(s => !s.IsDeleted && s.Group.TeacherId == u.Id),
            Sub = _db.UserSubscriptions
                .Where(s => s.UserId == u.Id && s.IsActive && (s.EndDate == null || s.EndDate > now))
                .OrderByDescending(s => s.EndDate)
                .Select(s => new { s.Plan, s.Platform, s.EndDate })
                .FirstOrDefault()
        });

        projected = sortByEssays
            ? projected.OrderByDescending(x => x.EssayCount).ThenByDescending(x => x.CreatedAt)
            : projected.OrderByDescending(x => x.CreatedAt);

        var rows = await projected
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new AdminUserItem(
            r.Id,
            r.FullName,
            r.Email,
            r.CreatedAt,
            r.LastLoginDate,
            r.IsDeleted,
            // Aktiv abunəliyi yoxdursa faktiki plan Free-dir.
            r.Sub is null ? SubscriptionPlan.Free : r.Sub.Plan,
            r.Sub?.Platform,
            r.Sub?.EndDate,
            r.Sub is not null && r.Sub.Platform == SubscriptionPlatform.GooglePlay,
            r.EssayCount,
            r.LastEssayAt,
            r.LessonCount,
            r.GroupCount,
            r.StudentCount)).ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new AdminUserListResponse(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<AdminActivityResponse> GetActivityAsync(
        AdminPeriod period, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var grouped = await _db.Essays
            .Where(e => e.CreatedAt >= fromUtc && e.CreatedAt < toUtc)
            .GroupBy(e => e.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Count = g.Count(),
                First = g.Min(e => e.CreatedAt),
                Last = g.Max(e => e.CreatedAt)
            })
            .OrderByDescending(g => g.Count)
            .ToListAsync(cancellationToken);

        var userIds = grouped.Select(g => g.UserId).ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.Email })
            .ToListAsync(cancellationToken);

        var lookup = users.ToDictionary(u => u.Id);

        var items = grouped.Select(g => new AdminActivityItem(
            g.UserId,
            lookup.TryGetValue(g.UserId, out var u) ? u.FullName : "(silinmiş istifadəçi)",
            lookup.TryGetValue(g.UserId, out var u2) ? u2.Email : null,
            g.Count,
            g.First,
            g.Last)).ToList();

        return new AdminActivityResponse(
            period,
            fromUtc,
            toUtc,
            items.Count,
            items.Sum(i => i.EssayCount),
            items);
    }
}
