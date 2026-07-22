namespace EssayChecker.Domain.Enums;

/// <summary>
/// AI-ın qaytardığı kateqoriyalar (frontend etiketləri: Qrammatika / Orfoqrafiya / Leksik / Təbii ifadə).
/// </summary>
public enum MistakeCategory
{
    Grammar = 0,
    Spelling = 1,
    Vocabulary = 2,
    NaturalExpression = 3
}
