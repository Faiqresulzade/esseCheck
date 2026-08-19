using EssayChecker.Domain.Enums;

namespace EssayChecker.Domain.Entities.Teaching;

/// <summary>
/// Müəllimin siyahısındakı şagird. Bu, tətbiqin istifadəçisi DEYİL — login-i, e-mailı, öz
/// kvotası yoxdur. Şagirdin bütün esselərini müəllim göndərir, kvota müəllimin hesabından gedir.
/// Şagird həmişə tam olaraq bir qrupa aiddir; sahiblik <see cref="StudentGroup.TeacherId"/>
/// üzərindən yoxlanılır.
/// </summary>
public class Student
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public string FullName { get; set; } = null!;

    /// <summary>
    /// Opsional sinif səviyyəsi. Təyin olunubsa, esse göndərilərkən sorğuda sinif
    /// göstərilməyəndə bu dəyər işlədilir (bax EssayService) — müəllim hər dəfə seçmək
    /// məcburiyyətində qalmır. Sorğuda açıq göndərilən sinif həmişə üstünlük təşkil edir.
    /// </summary>
    public GradeLevel? Grade { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Soft-delete: şagird siyahıdan çıxır, esseləri isə ona bağlı qalır.</summary>
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public StudentGroup Group { get; set; } = null!;
}
