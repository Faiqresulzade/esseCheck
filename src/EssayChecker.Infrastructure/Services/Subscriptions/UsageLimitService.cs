using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Subscriptions;
using EssayChecker.Application.Subscriptions;

namespace EssayChecker.Infrastructure.Services.Subscriptions;

/// <summary>
/// Mətnlə və şəkillə (OCR) yoxlama arasında fərq qoyulmur — hər ikisi eyni gündəlik limitə
/// (<see cref="PlanPolicy.DailyLimit"/>) sayılır. Text/Ocr sayğacları yalnız analitika üçün
/// ayrıca saxlanılır (bax DailyUsage.TextCheckCount/OcrCheckCount); limit yoxlaması hər zaman
/// ikisinin CƏMİNƏ görə aparılır.
/// </summary>
public sealed class UsageLimitService : IUsageLimitService
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IDailyUsageRepository _usageRepository;

    public UsageLimitService(
        ISubscriptionService subscriptionService,
        IDailyUsageRepository usageRepository)
    {
        _subscriptionService = subscriptionService;
        _usageRepository = usageRepository;
    }

    public Task<UsageDecision> CheckTextAsync(int userId, CancellationToken cancellationToken = default) =>
        CheckAsync(userId, cancellationToken);

    public Task ConsumeTextAsync(int userId, CancellationToken cancellationToken = default) =>
        _usageRepository.IncrementTextAsync(userId, TodayUtc(), cancellationToken);

    public Task<UsageDecision> CheckOcrAsync(int userId, CancellationToken cancellationToken = default) =>
        CheckAsync(userId, cancellationToken);

    public Task ConsumeOcrAsync(int userId, CancellationToken cancellationToken = default) =>
        _usageRepository.IncrementOcrAsync(userId, TodayUtc(), cancellationToken);

    public async Task<UsageDecision> CheckLessonAsync(int userId, CancellationToken cancellationToken = default)
    {
        var plan = await _subscriptionService.GetActivePlanAsync(userId, cancellationToken);
        var limit = PlanPolicy.LessonDailyLimit(plan);

        if (limit is null)
            return UsageDecision.Allow();

        var usage = await _usageRepository.GetAsync(userId, TodayUtc(), cancellationToken);

        return (usage?.LessonCount ?? 0) >= limit.Value
            ? UsageDecision.Deny($"Bugünkü dərs limitiniz ({limit}) bitib. Sabah yenilənəcək və ya planınızı yüksəldin.")
            : UsageDecision.Allow();
    }

    public Task ConsumeLessonAsync(int userId, CancellationToken cancellationToken = default) =>
        _usageRepository.IncrementLessonAsync(userId, TodayUtc(), cancellationToken);

    private async Task<UsageDecision> CheckAsync(int userId, CancellationToken cancellationToken)
    {
        var plan = await _subscriptionService.GetActivePlanAsync(userId, cancellationToken);
        var limit = PlanPolicy.DailyLimit(plan);

        if (limit is null)
            return UsageDecision.Allow();

        var used = await GetUsedTodayAsync(userId, cancellationToken);

        return used >= limit.Value
            ? UsageDecision.Deny($"Bugünkü limit ({limit}) bitib. Sabah yenilənəcək və ya planınızı yüksəldin.")
            : UsageDecision.Allow();
    }

    public async Task<DailyUsageStatusResponse> GetStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var plan = await _subscriptionService.GetActivePlanAsync(userId, cancellationToken);
        var limit = PlanPolicy.DailyLimit(plan);
        var lessonLimit = PlanPolicy.LessonDailyLimit(plan);

        var usage = await _usageRepository.GetAsync(userId, TodayUtc(), cancellationToken);
        var used = (usage?.TextCheckCount ?? 0) + (usage?.OcrCheckCount ?? 0);
        var lessonsUsed = usage?.LessonCount ?? 0;

        int? remaining = limit is null ? null : Math.Max(0, limit.Value - used);
        int? lessonRemaining = lessonLimit is null ? null : Math.Max(0, lessonLimit.Value - lessonsUsed);
        var resetAtUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);

        return new DailyUsageStatusResponse(
            plan,
            PlanPolicy.IsUnlimited(plan),
            limit,
            used,
            remaining,
            resetAtUtc,
            PlanPolicy.IsLessonUnlimited(plan),
            lessonLimit,
            lessonsUsed,
            lessonRemaining);
    }

    private async Task<int> GetUsedTodayAsync(int userId, CancellationToken cancellationToken)
    {
        var usage = await _usageRepository.GetAsync(userId, TodayUtc(), cancellationToken);
        return (usage?.TextCheckCount ?? 0) + (usage?.OcrCheckCount ?? 0);
    }

    private static DateOnly TodayUtc() => DateOnly.FromDateTime(DateTime.UtcNow);
}
