using EssayChecker.Domain.Entities.Essays;
using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EssayChecker.Persistence.Context;

public class EssayDbContext
: IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public EssayDbContext(DbContextOptions<EssayDbContext> options)
    : base(options)
    {
    }

    public DbSet<Essay> Essays => Set<Essay>();

    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();

    public DbSet<DailyUsage> DailyUsages => Set<DailyUsage>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ProcessedGoogleNotification> ProcessedGoogleNotifications => Set<ProcessedGoogleNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Bütün kodda DateTime.UtcNow istifadə olunur. PostgreSQL-in defolt "timestamp without
        // time zone" tipi Kind=Utc dəyərlərini yazarkən Npgsql-də istisna atır — buna görə bütün
        // DateTime/DateTime? sütunları açıq şəkildə "timestamp with time zone" (timestamptz) elan edilir.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    property.SetColumnType("timestamp with time zone");
            }
        }
    }
}
