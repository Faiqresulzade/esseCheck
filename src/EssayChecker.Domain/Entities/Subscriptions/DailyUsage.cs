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

    /// <summary>
    /// Yaradılmış dərs sayı. Esse sayğaclarından (Text/Ocr) TAM AYRIDIR — dərs limiti ayrıca
    /// hesablanır, bax PlanPolicy.LessonDailyLimit.
    /// </summary>
    public int LessonCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
