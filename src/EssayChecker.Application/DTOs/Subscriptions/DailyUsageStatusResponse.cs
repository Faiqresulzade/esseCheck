using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Subscriptions;

/// <summary>Əsas səhifədəki "Gündəlik pulsuz şans" bloku üçün.</summary>
public sealed record DailyUsageStatusResponse(
    SubscriptionPlan Plan,
    bool UnlimitedText,
    int? DailyTextLimit,
    int TextUsedToday,
    int? TextRemaining,
    bool CanUseOcr,
    DateTime ResetAtUtc);
