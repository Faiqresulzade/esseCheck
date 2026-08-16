using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Subscriptions;

public sealed record PlanInfoResponse(
    SubscriptionPlan Plan,
    string Name,
    decimal Price,
    string Currency,
    string Period,
    bool Unlimited,
    int? DailyLimit,
    IReadOnlyList<string> Features);
