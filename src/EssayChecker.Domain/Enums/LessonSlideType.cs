namespace EssayChecker.Domain.Enums;

/// <summary>
/// Dərs slaydının növü. Frontend hər növü fərqli şəkildə göstərir (formul, nümunə kartı,
/// iki sütunlu müqayisə və s.), ona görə siyahı qapalıdır — yeni növ əlavə etmək frontend
/// dəyişikliyi tələb edir.
/// </summary>
public enum LessonSlideType
{
    Intro = 0,
    Rule = 1,
    Examples = 2,
    Mistakes = 3,
    Compare = 4,
    Summary = 5
}
