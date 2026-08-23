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
        "apiKey", "brevoApiKey", "secret", "rtdnSharedSecret",
        // Cihaz identifikatoru DeviceTrials-də qəsdən heşlənir (şəxsi məlumatdır) — onu loga
        // açıq yazmaq həmin qorumanı mənasız edərdi.
        "deviceId", "integrityToken"
    };

    private static readonly Regex SensitiveFieldPattern = new(
        $@"(""(?:{string.Join("|", SensitiveKeys)})""\s*:\s*)""(?:[^""\\]|\\.)*""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Query string-dəki həssas parametrlər. RTDN endpoint-i paylaşılan sirri məhz query ilə alır
    /// (?secret=...) — o, body-də olmadığı üçün yuxarıdakı JSON şablonu onu tutmur və sirr
    /// RequestLogs cədvəlinə açıq mətn kimi düşürdü.
    /// </summary>
    private static readonly Regex SensitiveQueryPattern = new(
        $@"([?&](?:{string.Join("|", SensitiveKeys)})=)[^&]*",
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

    /// <summary>Query string-i maskalayır (bax <see cref="SensitiveQueryPattern"/>).</summary>
    public static string? SanitizeQueryString(string? queryString) =>
        string.IsNullOrEmpty(queryString)
            ? queryString
            : SensitiveQueryPattern.Replace(queryString, "$1***REDACTED***");
}
