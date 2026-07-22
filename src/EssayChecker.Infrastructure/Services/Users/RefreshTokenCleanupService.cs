using EssayChecker.Application.DTOs.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EssayChecker.Infrastructure.Services.Users;

/// <summary>
/// Vaxtı bitmiş refresh token-ləri dövri olaraq silir ki, <c>RefreshTokens</c> cədvəli
/// sonsuz böyüməsin. DbContext scoped olduğu üçün hər dövrədə yeni scope yaradılır.
/// </summary>
internal sealed class RefreshTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupService> logger)
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
                var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
                var deleted = await repository.DeleteExpiredAsync(DateTime.UtcNow, stoppingToken);

                if (deleted > 0)
                    _logger.LogInformation("Vaxtı bitmiş {Count} refresh token silindi.", deleted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Refresh token təmizləmə zamanı xəta baş verdi.");
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
