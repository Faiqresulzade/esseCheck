using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EssayChecker.Api.RateLimiting;

/// <summary>
/// Autentifikasiya endpoint-ləri üçün IP-yə görə sürət limiti.
///
/// Identity-nin öz lockout mexanizmi yalnız MƏLUM bir hesaba qarşı şifrə sınamasını dayandırır —
/// kütləvi qeydiyyatı, e-mail sıralamasını (enumeration) və ya şifrə sıfırlama e-mail bombasını
/// dayandırmır. Bu limitlər həmin boşluğu bağlayır.
///
/// Hədlər məktəb Wi-Fi kimi PAYLAŞILAN IP-ləri nəzərə alaraq seçilib: eyni şəbəkədən bir neçə
/// şagirdin qeydiyyatdan keçməsi normaldır, saatda 5 qeydiyyat bunu örtür.
/// </summary>
internal static class AuthRateLimiting
{
    public const string Registration = "auth-registration";
    public const string Login = "auth-login";
    public const string PasswordReset = "auth-password-reset";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Registration, PartitionByIp(permitLimit: 5, window: TimeSpan.FromHours(1)));
            options.AddPolicy(Login, PartitionByIp(permitLimit: 10, window: TimeSpan.FromMinutes(15)));
            options.AddPolicy(PasswordReset, PartitionByIp(permitLimit: 3, window: TimeSpan.FromHours(1)));

            // Limit aşıldıqda ProblemDetails yox, layihənin qalan hissəsi ilə eyni { message } forması.
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Həddindən çox cəhd edildi. Bir az sonra yenidən yoxlayın." },
                    cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// IP-yə görə bölünmüş sabit pəncərə. IP UseForwardedHeaders-dən SONRA oxunur, ona görə
    /// Render kimi proxy arxasında real istifadəçi IP-si görünür (bax Program.cs sıralaması).
    /// IP naməlum olsa hamısı bir "unknown" bölməsinə düşür — bu, qəsdən ehtiyatlı davranışdır.
    /// </summary>
    private static Func<HttpContext, RateLimitPartition<string>> PartitionByIp(int permitLimit, TimeSpan window) =>
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0
            });
}
