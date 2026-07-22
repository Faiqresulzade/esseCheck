using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Subscriptions;
using EssayChecker.Application.Subscriptions;

namespace EssayChecker.Infrastructure.Services.Subscriptions;

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

    public async Task<UsageDecision> CheckTextAsync(int userId, CancellationToken cancellationToken = default)
    {
        var plan = await _subscriptionService.GetActivePlanAsync(userId, cancellationToken);
        var limit = PlanPolicy.DailyTextLimit(plan);

        if (limit is null)
            return UsageDecision.Allow();

        var used = await GetTextUsedTodayAsync(userId, cancellationToken);

        return used >= limit.Value
            ? UsageDecision.Deny($"Bugünkü pulsuz limit ({limit}) bitib. Sabah yenilənəcək və ya Pro planına keçin.")
            : UsageDecision.Allow();
    }

    public Task ConsumeTextAsync(int userId, CancellationToken cancellationToken = default) =>
        _usageRepository.IncrementTextAsync(userId, TodayUtc(), cancellationToken);

    public async Task<UsageDecision> CheckOcrAsync(int userId, CancellationToken cancellationToken = default)
    {
        var plan = await _subscriptionService.GetActivePlanAsync(userId, cancellationToken);

        return PlanPolicy.CanUseOcr(plan)
            ? UsageDecision.Allow()
            : UsageDecision.Deny("Şəkildən esse oxuma yalnız Pro Plus üçün əlçatandır.");
    }

    public Task ConsumeOcrAsync(int userId, CancellationToken cancellationToken = default) =>
        _usageRepository.IncrementOcrAsync(userId, TodayUtc(), cancellationToken);

    public async Task<DailyUsageStatusResponse> GetStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var plan = await _subscriptionService.GetActivePlanAsync(userId, cancellationToken);
        var limit = PlanPolicy.DailyTextLimit(plan);
        var used = await GetTextUsedTodayAsync(userId, cancellationToken);

        int? remaining = limit is null ? null : Math.Max(0, limit.Value - used);
        var resetAtUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);

        return new DailyUsageStatusResponse(
            plan,
            PlanPolicy.UnlimitedText(plan),
            limit,
            used,
            remaining,
            PlanPolicy.CanUseOcr(plan),
            resetAtUtc);
    }

    private async Task<int> GetTextUsedTodayAsync(int userId, CancellationToken cancellationToken)
    {
        var usage = await _usageRepository.GetAsync(userId, TodayUtc(), cancellationToken);
        return usage?.TextCheckCount ?? 0;
    }

    private static DateOnly TodayUtc() => DateOnly.FromDateTime(DateTime.UtcNow);
}
