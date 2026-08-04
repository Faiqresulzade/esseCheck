namespace EssayChecker.Domain.Entities.Logs;

/// <summary>
/// Hər API sorğusu/cavabı üçün audit qeydi. Sütunlar sonradan filtrləmə üçün (istifadəçiyə,
/// endpoint-ə, status koduna, tarixə görə) düşünülüb — bax RequestLogConfiguration indeksləri.
/// </summary>
public class RequestLog
{
    public long Id { get; set; }

    /// <summary>Autentifikasiya olunmuş sorğuda JWT-dən; anonim sorğuda null.</summary>
    public int? UserId { get; set; }

    public string Method { get; set; } = null!;

    public string Path { get; set; } = null!;

    public string? QueryString { get; set; }

    public int StatusCode { get; set; }

    /// <summary>Şifrə/token kimi həssas sahələr yazılmazdan əvvəl maskalanır (bax RequestLogSanitizer).</summary>
    public string? RequestBody { get; set; }

    /// <summary>Eyni şəkildə maskalanmış, lazım gələrsə kəsilmiş cavab body-si.</summary>
    public string? ResponseBody { get; set; }

    public long DurationMs { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }
}
