using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using EssayChecker.Domain.Entities.Logs;
using EssayChecker.Persistence.Context;

namespace EssayChecker.Api.Logging;

/// <summary>
/// Hər /api sorğusunu (və cavabını) RequestLogs cədvəlinə yazır. Qəsdən pipeline-ın ən
/// başında (UseExceptionHandler-dən də əvvəl) qeydiyyatdan keçirilir ki, `finally` bloku
/// işlədikdə həm istisna işləyicisinin həll etdiyi son status kodu, həm də (daxildə
/// UseAuthentication artıq işlədiyi üçün) autentifikasiya olunmuş istifadəçi məlumatı əldə
/// edilə bilsin — HttpContext bütün pipeline boyu eyni obyektdir, middleware-in özünün pipeline-da
/// harada olması bunu dəyişmir, əsas olan `context.User`/`StatusCode`-u _next() TAMAMLANDIQDAN
/// SONRA oxumaqdır.
/// </summary>
internal sealed class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, EssayDbContext db)
    {
        // Swagger-in statik faylları (JS/CSS) məzmunca dəyərsizdir və sorğu sayını lüzumsuz artırır.
        if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request);

        var originalResponseStream = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            responseBuffer.Position = 0;
            var responseBody = await new StreamReader(responseBuffer, Encoding.UTF8).ReadToEndAsync();
            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalResponseStream);
            context.Response.Body = originalResponseStream;

            await SaveLogAsync(context, db, requestBody, responseBody, stopwatch.ElapsedMilliseconds);
        }
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
            return null;

        if (request.ContentType is null || !request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return "[JSON olmayan body (məs. fayl/şəkil) — loglanmır]";

        request.EnableBuffering();
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    private async Task SaveLogAsync(HttpContext context, EssayDbContext db, string? requestBody, string responseBody, long elapsedMs)
    {
        try
        {
            var log = new RequestLog
            {
                UserId = TryGetUserId(context.User),
                Method = context.Request.Method,
                Path = context.Request.Path.Value ?? string.Empty,
                QueryString = context.Request.QueryString.HasValue
                    ? RequestLogSanitizer.SanitizeQueryString(context.Request.QueryString.Value)
                    : null,
                StatusCode = context.Response.StatusCode,
                RequestBody = RequestLogSanitizer.Sanitize(requestBody),
                ResponseBody = RequestLogSanitizer.Sanitize(responseBody),
                DurationMs = elapsedMs,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            db.RequestLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Loglama heç vaxt əsl sorğunu poza bilməz — xəta yalnız qeyd olunur.
            _logger.LogError(ex, "Sorğu/cavab loglanarkən xəta baş verdi.");
        }
    }

    private static int? TryGetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}
