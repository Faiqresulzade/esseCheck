using EssayChecker.Domain.Enums;

namespace EssayChecker.Domain.Entities.Lessons;

/// <summary>
/// İstifadəçinin saxladığı bir dərs. Məzmun keşdən gəlsə belə hər istifadəçi üçün ayrıca sətir
/// yazılır — dərs onun öz siyahısında görünməli və silinə bilməlidir.
/// </summary>
public class Lesson
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>Dərs hansı şagird üçün qeyd olunub (opsional) — yalnız etiket/filtr məqsədilə.</summary>
    public int? StudentId { get; set; }

    /// <summary>İstifadəçinin yazdığı mövzu (göründüyü kimi saxlanılır).</summary>
    public string Topic { get; set; } = null!;

    /// <summary>
    /// Normallaşdırılmış mövzu (kiçik hərf, artıq boşluqlar təmizlənmiş). Eyni mövzunun təkrar
    /// soruşulduğunu tapmaq üçündür — bax LessonTopicKey.
    /// </summary>
    public string NormalizedTopic { get; set; } = null!;

    public GradeLevel Grade { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<LessonSlide> Slides { get; set; } = new();

    public List<LessonQuizQuestion> Quiz { get; set; } = new();
}
