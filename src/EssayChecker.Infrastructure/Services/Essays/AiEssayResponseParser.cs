using System.Text.Json;
using EssayChecker.Application.Common;

namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>AI-ın xam mətn cavabını gözlənilən cavab tipinə çevirir.</summary>
internal static class AiEssayResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <exception cref="JsonException">Cavab etibarlı JSON deyilsə — çağıran tərəf ehtiyat modelə keçir.</exception>
    public static T Parse<T>(string raw) where T : class
    {
        var json = ExtractJson(raw);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new AiServiceException("AI cavabı boş qayıtdı.", isTransient: true);
    }

    /// <summary>
    /// response_format dəstəklənəndə cavab onsuz da xam JSON-dur, amma ehtiyat model sxemi
    /// dəstəkləməyə bilər — ona görə ``` bloklarının təmizlənməsi yerində qalır.
    /// </summary>
    private static string ExtractJson(string content)
    {
        var text = content.Trim();

        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = text.IndexOf('\n');
            if (firstNewLine >= 0)
                text = text[(firstNewLine + 1)..];
            if (text.EndsWith("```", StringComparison.Ordinal))
                text = text[..^3];
            text = text.Trim();
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }
}
