using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EssayChecker.Infrastructure.Services.Logs;

/// <summary>
/// RequestLogs cədvəli hər sorğuda böyüdüyü üçün, saxlama müddəti (30 gün) bitmiş qeydləri
/// silir ki, cədvəl (və Render-in pulsuz Postgres yaddaşı) sonsuz böyüməsin.
/// </summary>
internal sealed class RequestLogCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RequestLogCleanupService> _logger;

    public RequestLogCleanupService(IServiceScopeFactory scopeFactory, ILogger<RequestLogCleanupService> logger)
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
                var repository = scope.ServiceProvider.GetRequiredService<IRequestLogRepository>();
                var cutoff = DateTime.UtcNow - RetentionPeriod;
                var deleted = await repository.DeleteOlderThanAsync(cutoff, stoppingToken);

                if (deleted > 0)
                    _logger.LogInformation("Saxlama müddəti (30 gün) bitmiş {Count} log qeydi silindi.", deleted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Log təmizləmə zamanı xəta baş verdi.");
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
