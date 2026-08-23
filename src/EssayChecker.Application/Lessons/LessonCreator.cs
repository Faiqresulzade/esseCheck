namespace EssayChecker.Application.Lessons;

/// <summary>
/// Dərsi yaradanın göstərilən adı. Hesab bərpaolunmaz silindikdə dərs kitabxanada qalır, amma
/// yaradan sahəsi boşalır (FK SetNull) — belə hallarda cavabda boş sətir yox, aydın əvəzedici
/// mətn qaytarılır ki, frontend ayrıca "yoxdursa nə yazım" məntiqi saxlamasın.
/// </summary>
public static class LessonCreator
{
    public const string DeletedDisplayName = "Silinmiş istifadəçi";

    public static string DisplayName(string? fullName) =>
        string.IsNullOrWhiteSpace(fullName) ? DeletedDisplayName : fullName;
}
