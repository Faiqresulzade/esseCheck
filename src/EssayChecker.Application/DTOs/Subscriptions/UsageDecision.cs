namespace EssayChecker.Application.DTOs.Subscriptions;

/// <summary>Limit yoxlamasının nəticəsi.</summary>
public sealed record UsageDecision(bool Allowed, string? Reason)
{
    public static UsageDecision Allow() => new(true, null);

    public static UsageDecision Deny(string reason) => new(false, reason);
}
