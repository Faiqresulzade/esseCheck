using System.Text.RegularExpressions;

namespace EssayChecker.Api.Logging;

/// <summary>
/// Log-a yazılmazdan əvvəl request/response body-lərindəki həssas sahələri (şifrə, token və s.)
/// maskalayır. Açıq mətn regex istifadə olunur ki, JSON-un formatı pozulmasın və deserializasiya
/// uğursuz olan/olmayan hər body üçün eyni şəkildə işləsin.
/// </summary>
internal static class RequestLogSanitizer
{
    private const int MaxStoredLength = 10_000;

    private static readonly string[] SensitiveKeys =
    {
        "password", "confirmPassword", "newPassword", "currentPassword",
        "token", "refreshToken", "purchaseToken", "accessToken",
        "apiKey", "brevoApiKey", "secret", "rtdnSharedSecret"
    };

    private static readonly Regex SensitiveFieldPattern = new(
        $@"(""(?:{string.Join("|", SensitiveKeys)})""\s*:\s*)""(?:[^""\\]|\\.)*""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string? Sanitize(string? body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        var redacted = SensitiveFieldPattern.Replace(body, @"$1""***REDACTED***""");

        return redacted.Length > MaxStoredLength
            ? redacted[..MaxStoredLength] + $"... [kəsildi, cəmi {redacted.Length} simvol]"
            : redacted;
    }
}
