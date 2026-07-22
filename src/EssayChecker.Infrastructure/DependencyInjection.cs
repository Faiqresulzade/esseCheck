using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Infrastructure.Ai;
using EssayChecker.Infrastructure.GooglePlay;
using EssayChecker.Infrastructure.Services.Essays;
using EssayChecker.Infrastructure.Services.Subscriptions;
using EssayChecker.Infrastructure.Services.Users;
using Microsoft.Extensions.DependencyInjection;

namespace EssayChecker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();

        // Essay / AI (OpenRouter)
        services.AddHttpClient<OpenRouterClient>();
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

        return services;
    }
}
