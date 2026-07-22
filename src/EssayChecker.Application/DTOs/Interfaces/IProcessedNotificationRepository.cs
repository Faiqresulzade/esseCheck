namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>RTDN mesajlarının idempotentliyi (eyni Pub/Sub mesajı iki dəfə emal olunmasın).</summary>
public interface IProcessedNotificationRepository
{
    /// <summary>Mesajı "emal olundu" kimi işarələyir. Artıq varsa false qaytarır (təkrardır, emal etmə).</summary>
    Task<bool> TryMarkProcessedAsync(string messageId, CancellationToken cancellationToken = default);
}
