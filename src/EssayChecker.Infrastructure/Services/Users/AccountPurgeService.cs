using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EssayChecker.Infrastructure.Services.Users;

/// <summary>
/// Silmə tələbindən (soft-delete) 30 gün ötmüş hesabları bərpaolunmaz şəkildə silir
/// (bax /legal/delete-account səhifəsindəki vəd). DbContext scoped olduğu üçün
/// hər dövrədə yeni scope yaradılır.
/// </summary>
internal sealed class AccountPurgeService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccountPurgeService> _logger;

    public AccountPurgeService(IServiceScopeFactory scopeFactory, ILogger<AccountPurgeService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAccountPurgeRepository>();
                var cutoff = DateTime.UtcNow - RetentionPeriod;
                var purged = await repository.PurgeExpiredDeletedAccountsAsync(cutoff, stoppingToken);

                if (purged > 0)
                    _logger.LogInformation("Saxlama müddəti (30 gün) bitmiş {Count} hesab bərpaolunmaz silindi.", purged);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Hesab təmizləmə (purge) zamanı xəta baş verdi.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
