namespace EssayChecker.Application.DTOs.Auth;

public enum LoginFailureReason
{
    InvalidCredentials,
    LockedOut
}

/// <summary>Login nəticəsi — uğur, ya da səbəbi ilə birlikdə uğursuzluq (lockout üçün ayrıca mesaj lazımdır).</summary>
public sealed record LoginOutcome(
    bool Succeeded,
    LoginFailureReason? FailureReason,
    LoginResponse? Response,
    DateTimeOffset? LockoutEndsAt)
{
    public static LoginOutcome Success(LoginResponse response) => new(true, null, response, null);

    public static LoginOutcome Invalid() => new(false, LoginFailureReason.InvalidCredentials, null, null);

    public static LoginOutcome LockedOut(DateTimeOffset? lockoutEndsAt) =>
        new(false, LoginFailureReason.LockedOut, null, lockoutEndsAt);
}
