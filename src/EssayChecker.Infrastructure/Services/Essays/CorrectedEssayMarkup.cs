using System.Text.RegularExpressions;
using EssayChecker.Application.DTOs.Essays;

namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>
/// correctedEssay mətnindəki &lt;b&gt;səhv&lt;/b&gt; (düzəliş) işarələmələrinin AI-dan asılı
/// olmadan düzgünlüyünü təmin edir. Prompt (Section 11) bu qaydaları AI-a izah etsə də, model
/// onları müntəzəm pozur — production-da hər iki pozuntu halı aşkarlanıb, ona görə nəticə
/// istifadəçiyə çatmazdan əvvəl burada məcburi düzəldilir.
/// </summary>
internal static class CorrectedEssayMarkup
{
    private static readonly Regex MarkupPairPattern =
        new(@"<b>(.*?)</b>\s*\((.*?)\)", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex BoldSpanPattern =
        new(@"<b>(.*?)</b>", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Əvvəlcə mənasız (səhv == düzəliş) işarələmələri təmizləyir, sonra siyahıda olub mətndə
    /// işarələnməmiş qalan səhvləri işarələyir.
    /// </summary>
    public static string Normalize(string correctedEssay, IReadOnlyList<EssayMistakeDto> mistakes) =>
        EnsureAllMistakesMarked(RemoveNoOpMarkup(correctedEssay), mistakes);

    /// <summary>
    /// Section 11 AI-a &lt;b&gt;X&lt;/b&gt; (X) kimi eyni cütləri təmizləməyi əmr edir, amma model
    /// bəzən buna əməl etmir — Section 5-dəki eyni no-op problemi correctedEssay mətnində də baş
    /// verir. Hər cütü tap və "wrong" ilə "correct" trim edilmiş halda eynidirsə, işarələməni
    /// çıxarıb sadəcə orijinal sözü saxlayırıq.
    /// </summary>
    private static string RemoveNoOpMarkup(string correctedEssay) =>
        MarkupPairPattern.Replace(correctedEssay, match =>
        {
            var wrong = match.Groups[1].Value;
            var correct = match.Groups[2].Value;
            return string.Equals(wrong.Trim(), correct.Trim(), StringComparison.Ordinal) ? wrong : match.Value;
        });

    /// <summary>
    /// Promptda (Section 11) AI-a hər mistakes elementini correctedEssay-də &lt;b&gt;wrong&lt;/b&gt;
    /// (correct) kimi işarələməyi əmr edirik, amma model bəzən bunu unudur (istifadəçi
    /// production-da "really mindful" → "mindful" kimi bir səhvin siyahıda olub, mətndə
    /// işarələnməmiş qaldığını tapıb) — bu, statistikanın tutarlı görünüb, amma vizual
    /// işarələmənin əskik olması şəklində qarışıqlıq yaradır.
    /// </summary>
    private static string EnsureAllMistakesMarked(string correctedEssay, IReadOnlyList<EssayMistakeDto> mistakes)
    {
        if (mistakes.Count == 0 || string.IsNullOrEmpty(correctedEssay))
            return correctedEssay;

        foreach (var mistake in mistakes)
        {
            if (string.IsNullOrEmpty(mistake.Wrong) || IsAlreadyMarked(correctedEssay, mistake.Wrong))
                continue;

            var wrongIndex = FindUnmarkedOccurrence(correctedEssay, mistake.Wrong);
            if (wrongIndex >= 0)
            {
                correctedEssay = InsertMarkup(correctedEssay, wrongIndex, mistake.Wrong.Length, mistake);
                continue;
            }

            // "wrong" mətni tapılmadı — bu, tez-tez AI-ın Section 11-i pozaraq düzəlişi
            // sükutla mətnə yazıb ("wrong"-u "correct" ilə əvəz edib), işarələməyi unutması
            // deməkdir. Bu halda "correct" mətnini axtarıb, onu <b>wrong</b> (correct) ilə
            // əvəz edirik ki, hər iki tərəf (orijinal və düzəliş) görünsün.
            if (string.IsNullOrEmpty(mistake.Correct))
                continue;

            var correctIndex = FindUnmarkedOccurrence(correctedEssay, mistake.Correct);
            if (correctIndex < 0)
                continue; // Nə "wrong", nə "correct" tapılmadı — sükutla keçirik, statistics/mistakes hələ də düzgündür.

            correctedEssay = InsertMarkup(correctedEssay, correctIndex, mistake.Correct.Length, mistake);
        }

        return correctedEssay;
    }

    private static bool IsAlreadyMarked(string correctedEssay, string wrong) =>
        BoldSpanPattern.Matches(correctedEssay)
            .Any(m => string.Equals(m.Groups[1].Value.Trim(), wrong.Trim(), StringComparison.Ordinal));

    private static string InsertMarkup(string correctedEssay, int index, int length, EssayMistakeDto mistake) =>
        string.Concat(
            correctedEssay.AsSpan(0, index),
            $"<b>{mistake.Wrong}</b> ({mistake.Correct})",
            correctedEssay.AsSpan(index + length));

    /// <summary>Verilən mətnin correctedEssay-də hələ &lt;b&gt;&lt;/b&gt; içinə alınmamış ilk yerini tapır.</summary>
    private static int FindUnmarkedOccurrence(string correctedEssay, string value)
    {
        var searchStart = 0;
        while (true)
        {
            var index = correctedEssay.IndexOf(value, searchStart, StringComparison.Ordinal);
            if (index < 0)
                return -1;

            var lastOpenTag = correctedEssay.LastIndexOf("<b>", index, StringComparison.Ordinal);
            var lastCloseTag = correctedEssay.LastIndexOf("</b>", index, StringComparison.Ordinal);
            var isInsideBoldTag = lastOpenTag > lastCloseTag;

            if (!isInsideBoldTag)
                return index;

            searchStart = index + value.Length;
            if (searchStart >= correctedEssay.Length)
                return -1;
        }
    }
}
