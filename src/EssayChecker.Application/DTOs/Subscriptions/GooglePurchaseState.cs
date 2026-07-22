namespace EssayChecker.Application.DTOs.Subscriptions;

/// <summary>Google-dan alınmış, təsdiqlənmiş satınalma vəziyyəti (source of truth).</summary>
public sealed record GooglePurchaseState(
    bool IsActive,
    DateTime ExpiryUtc,
    bool AutoRenewing,
    bool IsAcknowledged);
