namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>
/// Cümlə "Because" ilə başlayanda (fraqment/stilistik problem) bu artıq AI-a etibar edilmir —
/// deterministik şəkildə "As" / "Since" / "This is because" sinonimlərindən biri ilə əvəz olunur.
/// "Because of ..." toxunulmur (fərqli, həmişə düzgün konstruksiyadır — sinonimlər onun yerinə keçmir).
/// </summary>
internal static class SentenceInitialBecauseRule
{
    private static readonly string[] Synonyms = { "As", "Since", "This is because" };

    public sealed record Violation(int Index, string Wrong);

    /// <summary>Hər çağırışda (yəni hər esse qiymətləndirməsində) TƏK bir sinonim seçilir və
    /// mətndəki bütün "Because" nüsxələrinə eyni şəkildə tətbiq olunur — hər nüsxəyə ayrı-ayrı
    /// təsadüfi sinonim vermək eyni "wrong" mətninin fərqli düzəlişlərlə üst-üstə düşməsinə
    /// (CorrectedEssayMarkup-un axtarış-əsaslı işarələmə mexanizmi bunu dəstəkləmir) səbəb olardı.</summary>
    public static List<Violation> FindViolations(string essayText)
    {
        var violations = new List<Violation>();
        if (string.IsNullOrEmpty(essayText))
            return violations;

        foreach (var sentenceStart in SentenceBoundaries.FindStartIndexes(essayText))
        {
            if (!MatchesBecause(essayText, sentenceStart, out var wrong))
                continue;

            violations.Add(new Violation(sentenceStart, wrong));
        }

        return violations;
    }

    /// <summary>Verilmiş "wrong" mətninin (böyük/kiçik hərfinə uyğun) təsadüfi seçilmiş sinonimini qaytarır.</summary>
    public static string PickSynonym(string wrong, Random random)
    {
        var synonym = Synonyms[random.Next(Synonyms.Length)];
        return char.IsUpper(wrong[0]) ? synonym : char.ToLowerInvariant(synonym[0]) + synonym[1..];
    }

    private static bool MatchesBecause(string text, int index, out string wrong)
    {
        wrong = string.Empty;
        const string word = "Because";

        if (index + word.Length > text.Length)
            return false;

        if (string.Compare(text, index, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
            return false;

        var after = index + word.Length;

        // "Because" özbaşına bir söz olmalıdır, "Becausexyz" kimi bir sözün başlanğıcı yox.
        if (after < text.Length && char.IsLetter(text[after]))
            return false;

        // "Because of ..." fərqli, həmişə düzgün bir konstruksiyadır — toxunmuruq.
        var afterSpace = SkipSpaces(text, after);
        if (afterSpace + 2 <= text.Length
            && string.Compare(text, afterSpace, "of", 0, 2, StringComparison.OrdinalIgnoreCase) == 0
            && (afterSpace + 2 == text.Length || !char.IsLetter(text[afterSpace + 2])))
            return false;

        wrong = text.Substring(index, word.Length);
        return true;
    }

    private static int SkipSpaces(string text, int index)
    {
        while (index < text.Length && text[index] == ' ')
            index++;

        return index;
    }
}
