using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Subscriptions;

public sealed record SubscriptionResponse(
    SubscriptionPlan Plan,
    bool IsActive,
    DateTime? StartDate,
    DateTime? EndDate,
    bool AutoRenew,
    SubscriptionPlatform? Platform);
