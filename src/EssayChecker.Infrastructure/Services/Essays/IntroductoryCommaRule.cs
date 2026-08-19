namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>
/// Cümlə əvvəlində gələn keçid ifadəsindən sonra vergül qaydası (əvvəllər prompt-un P1 qaydası)
/// artıq AI-a etibar edilmir — AI bunu "mistakes" massivinə əlavə etməyi tez-tez unudur, vergülü
/// sükutla mətnə qoyub işarələməni buraxırdı (production-da təkrar-təkrar müşahidə olunub).
/// Bu sinif isə tam deterministikdir: heç bir AI cavabından asılı deyil.
/// </summary>
internal static class IntroductoryCommaRule
{
    /// <summary>İstifadəçinin təsdiqlədiyi dəqiq siyahı — uzunluğa görə azalan sırada, ki uzun
    /// ifadə qısa bir prefiksin yanlış uyğunluğuna düşməsin (məs. "As a result" "As"-dan əvvəl yoxlanılsın).</summary>
    private static readonly string[] Phrases = new[]
    {
        "As far as I am concerned", "From my point of view", "For this reason",
        "On the other hand", "As a result", "In my opinion", "First of all",
        "To begin with", "What is more", "In particular", "To sum up", "In summary",
        "In contrast", "By contrast", "In addition", "In conclusion", "To conclude",
        "All in all", "For example", "For instance", "In my view", "Nevertheless",
        "Nonetheless", "Consequently", "Additionally", "Furthermore", "Therefore",
        "Meanwhile", "Otherwise", "Firstly", "Secondly", "Thirdly", "Finally",
        "Lastly", "However", "Overall", "In fact", "Instead", "Thus", "Hence",
    }.OrderByDescending(p => p.Length).ToArray();

    public sealed record Violation(int Index, string Phrase);

    /// <summary>
    /// <paramref name="essayText"/> daxilində cümlə əvvəlində gəlib, amma dərhal sonra vergül
    /// olmayan bütün keçid ifadəsi nüsxələrini tapır (böyük/kiçik hərfdən asılı olmadan).
    /// </summary>
    public static List<Violation> FindViolations(string essayText)
    {
        var violations = new List<Violation>();
        if (string.IsNullOrEmpty(essayText))
            return violations;

        foreach (var sentenceStart in SentenceBoundaries.FindStartIndexes(essayText))
        {
            var phrase = MatchPhraseAt(essayText, sentenceStart);
            if (phrase is null)
                continue;

            var afterPhrase = sentenceStart + phrase.Length;
            if (afterPhrase < essayText.Length && essayText[afterPhrase] == ',')
                continue; // Artıq vergül var — bu nüsxə düzgündür, toxunmuruq.

            violations.Add(new Violation(sentenceStart, essayText.Substring(sentenceStart, phrase.Length)));
        }

        return violations;
    }

    /// <summary>Uyğunluq böyük/kiçik hərfdən asılı olmadan yoxlanılır, amma qaytarılan mətn
    /// tələbənin orijinal yazdığı formanı (case) saxlayır — baş hərfi "düzəltmirik".</summary>
    private static string? MatchPhraseAt(string text, int index)
    {
        foreach (var phrase in Phrases)
        {
            if (index + phrase.Length > text.Length)
                continue;

            if (string.Compare(text, index, phrase, 0, phrase.Length, StringComparison.OrdinalIgnoreCase) != 0)
                continue;

            // Söz sərhədi: ifadədən dərhal sonra hərf gəlməməlidir (təsadüfi uzun bir sözün
            // başlanğıcı ilə yanlış uyğunluğun qarşısını almaq üçün, məs. "Thusly").
            var after = index + phrase.Length;
            if (after < text.Length && char.IsLetter(text[after]))
                continue;

            return phrase;
        }

        return null;
    }

}
