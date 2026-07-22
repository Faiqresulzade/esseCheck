namespace EssayChecker.Domain.Entities.Subscriptions;

/// <summary>
/// RTDN (Real-time Developer Notifications) Pub/Sub mesajlarının idempotentliyi üçün.
/// Eyni messageId təkrar gəlsə (Pub/Sub "at-least-once" çatdırır), ikinci dəfə emal olunmasın.
/// </summary>
public class ProcessedGoogleNotification
{
    public int Id { get; set; }

    public string MessageId { get; set; } = null!;

    public DateTime ProcessedAt { get; set; }
}
