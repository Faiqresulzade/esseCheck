using EssayChecker.Domain.Enums;

namespace EssayChecker.Domain.Entities.Lessons;

/// <summary>
/// Keş: bir dəfə yaradılmış dərs məzmunu. Eyni mövzu+sinif başqa istifadəçi tərəfindən
/// soruşulanda AI yenidən çağırılmır, məzmun buradan kopyalanır.
///
/// <see cref="PromptVersion"/> açarın bir hissəsidir: prompt dəyişəndə versiya artırılır və
/// köhnə şablonlar avtomatik yararsız olur (silmək lazım deyil, sadəcə bir daha tapılmırlar).
/// Bu, "prompt yaxşılaşdırıldı, amma hamı köhnə dərsi almağa davam edir" problemini aradan
/// qaldırır.
/// </summary>
public class LessonTemplate
{
    public int Id { get; set; }

    public string NormalizedTopic { get; set; } = null!;

    /// <summary>İlk yaradılışdakı mövzu mətni — yeni dərslərə başlıq kimi köçürülür.</summary>
    public string Topic { get; set; } = null!;

    public GradeLevel Grade { get; set; }

    public int PromptVersion { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<LessonSlide> Slides { get; set; } = new();

    public List<LessonQuizQuestion> Quiz { get; set; } = new();
}
