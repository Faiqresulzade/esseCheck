namespace EssayChecker.Domain.Entities.Marketing;

/// <summary>
/// Tərəfdaşlıq/tanıtım üçün toplanan müəllim əlaqələri (məs. tədris platformalarından əl ilə
/// yığılır) — sonradan onlara EssayCheck AI haqqında mesaj göndərmək üçün istifadə olunur.
/// </summary>
public class EnglishTeacher
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? City { get; set; }

    /// <summary>Müəllimin tapıldığı elan/profil linki (məs. tədris platforması).</summary>
    public string? ProfileUrl { get; set; }

    public string? Notes { get; set; }

    public bool IsContacted { get; set; }

    public DateTime? ContactedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
