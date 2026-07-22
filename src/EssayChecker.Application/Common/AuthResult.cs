namespace EssayChecker.Application.Common;

/// <summary>
/// Auth əməliyyatlarının nəticəsini (uğur/xəta) daşıyan sadə model.
/// </summary>
public class AuthResult
{
    public bool Succeeded { get; init; }

    public string Message { get; init; } = string.Empty;

    public IEnumerable<string> Errors { get; init; } = Enumerable.Empty<string>();

    public static AuthResult Success(string message = "") =>
        new() { Succeeded = true, Message = message };

    public static AuthResult Failure(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}
