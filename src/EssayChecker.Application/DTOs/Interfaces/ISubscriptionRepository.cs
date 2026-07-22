using EssayChecker.Domain.Entities.Subscriptions;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface ISubscriptionRepository
{
    Task<UserSubscription?> GetActiveAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Verilən purchaseToken (və ya onun LinkedPurchaseToken zəncirinin) sahibini tapır (RTDN üçün).</summary>
    Task<UserSubscription?> GetByPurchaseTokenAsync(string purchaseToken, CancellationToken cancellationToken = default);

    Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>İstifadəçinin bütün aktiv abunəliklərini deaktiv edir.</summary>
    Task DeactivateAllAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default);

    /// <summary>İstifadəçinin "keepSubscriptionId"-dən başqa bütün aktiv abunəliklərini deaktiv edir (RTDN reconciliation üçün).</summary>
    Task DeactivateOthersAsync(int userId, int keepSubscriptionId, DateTime utcNow, CancellationToken cancellationToken = default);
}
