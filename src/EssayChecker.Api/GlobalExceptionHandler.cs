using EssayChecker.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace EssayChecker.Api;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "İşlənməmiş xəta baş verdi.");

        var (status, message) = exception switch
        {
            AiServiceException { IsTransient: true } =>
                (StatusCodes.Status503ServiceUnavailable, "AI xidməti hazırda məşğuldur, bir azdan yenidən cəhd edin."),
            AiServiceException =>
                (StatusCodes.Status502BadGateway, "AI xidməti ilə əlaqə alınmadı."),
            _ =>
                (StatusCodes.Status500InternalServerError, "Gözlənilməz xəta baş verdi.")
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken);
        return true;
    }
}
