using System.Net.Http;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using EssayChecker.Infrastructure.Ai;
using EssayChecker.Infrastructure.GooglePlay;
using EssayChecker.Infrastructure.Services.Essays;
using EssayChecker.Infrastructure.Services.Logs;
using EssayChecker.Infrastructure.Services.Subscriptions;
using EssayChecker.Infrastructure.Services.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();

        // E-mail: Email:BrevoApiKey doldurulubsa Brevo-nun HTTPS API-si, əks halda SMTP işlədilir.
        // Render kimi platformalar çıxan SMTP portlarını bloklayır, ona görə production-da Brevo
        // istifadə olunur; lokal development-də açar boş qalır və SMTP ilə davam edilir.
        services.AddScoped<EmailService>();
        services.AddHttpClient<BrevoEmailService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddScoped<IEmailService>(sp =>
        {
            var emailSettings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;
            return string.IsNullOrWhiteSpace(emailSettings.BrevoApiKey)
                ? sp.GetRequiredService<EmailService>()
                : sp.GetRequiredService<BrevoEmailService>();
        });

        // Essay / AI (OpenRouter) — timeout müəyyənləşdirilib ki, AI yavaşlasa
        // istifadəçi sonsuz gözləmək əvəzinə təmiz xəta (503) alsın.
        services.AddHttpClient<OpenRouterClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        // Defolt SocketsHttpHandler hər bağlantı üçün Windows-un sistem proksi aşkarlanmasını
        // (WPAD/PAC axtarışı) işə salır — bu, bəzi Windows dev mühitlərində hər sorğuya
        // onlarla saniyə əlavə edə bilir (real şəbəkə gecikməsi deyil, proksi axtarışının
        // özüdür). Bizə heç bir korporativ proksi lazım deyil, ona görə tam söndürülür.
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            UseProxy = false,
            Proxy = null
        });
        services.AddScoped<IEssayEvaluator, OpenRouterEssayEvaluator>();
        services.AddScoped<IOcrService, OpenRouterOcrService>();
        services.AddScoped<IEssayService, EssayService>();

        // Subscription / Daily limit
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IUsageLimitService, UsageLimitService>();

        // Google Play Billing (server-side satınalma təsdiqi)
        services.AddSingleton<IGooglePlayPurchaseVerifier, GooglePlayPurchaseVerifier>();

        // Fon xidmətləri
        services.AddHostedService<RefreshTokenCleanupService>();
        services.AddHostedService<AccountPurgeService>();
        services.AddHostedService<RequestLogCleanupService>();

        return services;
    }
}
