using EssayChecker.Domain.Enums;

namespace EssayChecker.Domain.Entities.Lessons;

/// <summary>
/// Bir mövzu izahı — ORTAQ kitabxananın sətri, istifadəçiyə aid deyil.
///
/// Bir mövzu+sinif cütü üçün yalnız BİR dərs mövcud olur (unikal indeks) və onu bütün müəllimlər
/// görür. Səbəb xərcdir: eyni mövzunu hər müəllim üçün yenidən yaratmaq token israfıdır.
/// <see cref="CreatedByUserId"/> yalnız "bunu kim yaratdı" məlumatıdır — sahiblik hüququ vermir,
/// dərs silinmir (bax LessonsController).
/// </summary>
public class Lesson
{
    public int Id { get; set; }

    /// <summary>
    /// Dərsi ilk dəfə yaradan (gündəlik limiti xərcləyən) istifadəçi.
    ///
    /// Hesab bərpaolunmaz silindikdə null olur (FK SetNull) — dərsin ÖZÜ silinmir, çünki o,
    /// ortaq resursdur və başqa müəllimlər ondan istifadə edir. Əvvəllər burada Restrict var idi,
    /// amma o, hesab təmizləmə xidmətini (AccountPurgeService) tamamilə bloklayırdı.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Yaradanın yazdığı mövzu (göründüyü kimi saxlanılır).</summary>
    public string Topic { get; set; } = null!;

    /// <summary>
    /// Normallaşdırılmış mövzu (kiçik hərf, artıq boşluqlar təmizlənmiş) — kitabxanada eyni
    /// mövzunun təkrar yaradılmasının qarşısını alan açar. Bax LessonTopicKey.
    /// </summary>
    public string NormalizedTopic { get; set; } = null!;

    public GradeLevel Grade { get; set; }

    /// <summary>
    /// Bu məzmun hansı prompt versiyası ilə yaradılıb (bax LessonPrompts.Version).
    /// QƏSDƏN avtomatik köhnəlmə yoxdur: versiya artanda mövcud dərs yenidən yaradılmır, çünki
    /// bu, mövzunu yazan müəllimin yeganə gündəlik limitini onsuz da mövcud olan dərsə xərcləyərdi.
    /// Sahə köhnə dərsləri görüb məqsədli şəkildə təmizləmək üçündür.
    /// </summary>
    public int PromptVersion { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<LessonSlide> Slides { get; set; } = new();

    public List<LessonQuizQuestion> Quiz { get; set; } = new();
}
