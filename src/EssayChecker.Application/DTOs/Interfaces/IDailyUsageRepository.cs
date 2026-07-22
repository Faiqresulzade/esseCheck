using EssayChecker.Domain.Entities.Subscriptions;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface IDailyUsageRepository
{
    Task<DailyUsage?> GetAsync(int userId, DateOnly usageDate, CancellationToken cancellationToken = default);

    /// <summary>Verilən gün üçün text sayğacını +1 edir (yoxdursa yaradır).</summary>
    Task IncrementTextAsync(int userId, DateOnly usageDate, CancellationToken cancellationToken = default);

    /// <summary>Verilən gün üçün OCR sayğacını +1 edir (yoxdursa yaradır).</summary>
    Task IncrementOcrAsync(int userId, DateOnly usageDate, CancellationToken cancellationToken = default);
}
