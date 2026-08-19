namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>Cümlə başlanğıcı indekslərini tapmaq üçün [[IntroductoryCommaRule]] və
/// [[SentenceInitialBecauseRule]] tərəfindən paylaşılan məntiq.</summary>
internal static class SentenceBoundaries
{
    /// <summary>Mətnin əvvəli və hər cümlə/sətir sonundan dərhal sonra gələn indekslər
    /// (təkrarsız — eyni indeks bir neçə sərhəd səbəbindən, məs. ". " sonra "\n", təkrar yarana bilər).</summary>
    public static IEnumerable<int> FindStartIndexes(string text)
    {
        var seen = new HashSet<int>();

        void TryYield(int index, List<int> into)
        {
            if (seen.Add(index))
                into.Add(index);
        }

        var result = new List<int>();
        TryYield(SkipWhitespace(text, 0), result);

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] is '.' or '!' or '?' or '\n')
            {
                var j = i + 1;
                while (j < text.Length && text[j] is '.' or '!' or '?' or '"' or '\'')
                    j++;

                TryYield(SkipWhitespace(text, j), result);
            }
        }

        return result;
    }

    private static int SkipWhitespace(string text, int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index]))
            index++;

        return index;
    }
}
