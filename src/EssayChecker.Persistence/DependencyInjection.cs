using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Domain.Entities.Users;
using EssayChecker.Persistence.Context;
using EssayChecker.Persistence.Identity;
using EssayChecker.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EssayChecker.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EssayDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = true;

            // Frontend tələbi: ən azı 8 simvol, böyük hərf, kiçik hərf və rəqəm.
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;

            // Brute-force qarşısı: 5 yanlış cəhddən sonra 15 dəqiqəlik müvəqqəti bloklama.
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole<int>>()
        .AddEntityFrameworkStores<EssayDbContext>()
        .AddDefaultTokenProviders()
        .AddErrorDescriber<AzerbaijaniIdentityErrorDescriber>();

        services.AddScoped<IEssayRepository, EssayRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IDailyUsageRepository, DailyUsageRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProcessedNotificationRepository, ProcessedNotificationRepository>();

        return services;
    }
}
