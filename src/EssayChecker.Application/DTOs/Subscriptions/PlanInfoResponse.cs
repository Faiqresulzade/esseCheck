using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Subscriptions;

public sealed record PlanInfoResponse(
    SubscriptionPlan Plan,
    string Name,
    decimal Price,
    string Currency,
    string Period,
    bool UnlimitedText,
    int? DailyTextLimit,
    bool Ocr,
    IReadOnlyList<string> Features);
