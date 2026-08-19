namespace EssayChecker.Domain.Entities.Teaching;

/// <summary>
/// Müəllimin yaratdığı şagird qrupu (məs. "11-A İngilis"). Qrup tam olaraq bir müəllimə aiddir
/// və başqa istifadəçiyə köçürülmür — şagirdin sahibliyi buradan, <see cref="TeacherId"/>
/// üzərindən müəyyən olunur.
/// </summary>
public class StudentGroup
{
    public int Id { get; set; }

    /// <summary>Qrupu yaradan istifadəçi (AppUser.Id).</summary>
    public int TeacherId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Soft-delete: qrup silinəndə şagirdləri də soft-delete olunur, amma esse tarixçəsi
    /// toxunulmur — keçmiş qiymətləndirmələr (və gələcək inkişaf analitikası) itməməlidir.
    /// </summary>
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public List<Student> Students { get; set; } = new();
}
