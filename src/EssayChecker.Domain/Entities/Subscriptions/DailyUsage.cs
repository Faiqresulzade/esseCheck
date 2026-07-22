namespace EssayChecker.Domain.Entities.Subscriptions;

/// <summary>
/// İstifadəçinin bir UTC günü üzrə istifadə sayğacları. Hər gün üçün ayrıca sətir
/// yaradılır — beləliklə limit avtomatik sıfırlanır.
/// </summary>
public class DailyUsage
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>İstifadə günü (UTC).</summary>
    public DateOnly UsageDate { get; set; }

    public int TextCheckCount { get; set; }

    public int OcrCheckCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
