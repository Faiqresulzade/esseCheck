using System.Text.RegularExpressions;
using EssayChecker.Application.DTOs.Essays;

namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>
/// correctedEssay mətnindəki &lt;b&gt;səhv&lt;/b&gt; (düzəliş) işarələmələrinin AI-dan asılı
/// olmadan düzgünlüyünü təmin edir. Prompt bu qaydaları AI-a izah etsə də, model onları
/// müntəzəm pozur — production-da hər pozuntu halı aşkarlanıb, ona görə nəticə istifadəçiyə
/// çatmazdan əvvəl burada məcburi düzəldilir.
/// </summary>
internal static class CorrectedEssayMarkup
{
    /// <summary>Bir səhv üçün ən çox neçə yerə işarə qoyula bilər — sonsuz loopa qarşı sərhəd.</summary>
    private const int MaxMarkupInsertionsPerMistake = 20;

    private static readonly Regex MarkupPairPattern =
        new(@"<b>(.*?)</b>\s*\((.*?)\)", RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex BoldSpanPattern =
        new(@"<b>(.*?)</b>", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Əvvəlcə mənasız (səhv == düzəliş) işarələmələri təmizləyir, sonra siyahıda olub mətndə
    /// işarələnməmiş qalan bütün səhvləri işarələyir.
    /// </summary>
    public static string Normalize(string correctedEssay, IReadOnlyList<EssayMistakeDto> mistakes) =>
        EnsureAllMistakesMarked(RemoveNoOpMarkup(correctedEssay), mistakes);

    /// <summary>
    /// Prompt AI-a &lt;b&gt;X&lt;/b&gt; (X) kimi eyni cütləri təmizləməyi əmr edir, amma model
    /// bəzən buna əməl etmir. Hər cütü tap və "wrong" ilə "correct" trim edilmiş halda
    /// eynidirsə, işarələməni çıxarıb sadəcə orijinal sözü saxlayırıq.
    /// </summary>
    private static string RemoveNoOpMarkup(string correctedEssay) =>
        MarkupPairPattern.Replace(correctedEssay, match =>
        {
            var wrong = match.Groups[1].Value;
            var correct = match.Groups[2].Value;
            return string.Equals(wrong.Trim(), correct.Trim(), StringComparison.Ordinal) ? wrong : match.Value;
        });

    /// <summary>
    /// Promptda AI-a hər mistakes elementini correctedEssay-də &lt;b&gt;wrong&lt;/b&gt; (correct)
    /// kimi işarələməyi əmr edirik, amma model bunu iki cür poza bilir: (a) işarələməyi tamamilə
    /// unudur, (b) düzəlişi sükutla mətnə tətbiq edir, işarə qoymur. Hər iki hal burada bərpa
    /// olunur — həm də səhvin BÜTÜN təkrarları üçün, tək birinci yer üçün deyil.
    /// </summary>
    private static string EnsureAllMistakesMarked(string correctedEssay, IReadOnlyList<EssayMistakeDto> mistakes)
    {
        if (mistakes.Count == 0 || string.IsNullOrEmpty(correctedEssay))
            return correctedEssay;

        foreach (var mistake in mistakes)
        {
            if (string.IsNullOrEmpty(mistake.Wrong))
                continue;

            correctedEssay = MarkAllOccurrences(correctedEssay, mistake);
        }

        return correctedEssay;
    }

    /// <summary>
    /// Səhvin işarələnməmiş qalan hər təkrarını işarələyir. Axtarış hər dəfə əlavə edilmiş
    /// işarələmədən SONRA davam etdiyi üçün irəliləyiş zəmanətlidir (sonsuz loop mümkün deyil),
    /// üstəlik <see cref="MaxMarkupInsertionsPerMistake"/> sərhədi var.
    /// </summary>
    private static string MarkAllOccurrences(string text, EssayMistakeDto mistake)
    {
        var searchFrom = 0;

        for (var inserted = 0; inserted < MaxMarkupInsertionsPerMistake; inserted++)
        {
            var index = FindUnmarkedOccurrence(text, mistake.Wrong, searchFrom);
            int length;

            if (index >= 0)
            {
                // Model düzəlişi işarələmədən, hazır halda yazmış ola bilər (məs. orijinalda
                // "In my opinion" ikən mətndə "In my opinion,"). Belə halda "wrong" "correct"-in
                // prefiksi olduğu üçün axtarış məhz düzəldilmiş mətnin üstünə düşür — yalnız
                // "wrong" qədərini əvəz etsək, düzəlişin quyruğu (vergül) mətndə qalıb
                // təkrarlanardı: "(In my opinion,), AI". Ona görə tam "correct"-i əvəz edirik.
                length = IsCorrectedTextAt(text, index, mistake.Correct, mistake.Wrong)
                    ? mistake.Correct.Length
                    : mistake.Wrong.Length;
            }
            else
            {
                // "wrong" heç tapılmadı — model onu bütövlüklə "correct" ilə əvəz edib.
                if (string.IsNullOrEmpty(mistake.Correct))
                    return text;

                index = FindUnmarkedOccurrence(text, mistake.Correct, searchFrom);
                if (index < 0)
                    return text; // Nə biri, nə digəri tapılmadı — statistics/mistakes yenə düzgündür.

                length = mistake.Correct.Length;
            }

            text = InsertMarkupAt(text, index, length, mistake);
            searchFrom = index + MarkupLength(mistake);
        }

        return text;
    }

    /// <summary>
    /// <paramref name="index"/> mövqeyində orijinal ("wrong") mətn deyil, artıq tətbiq edilmiş
    /// düzəliş ("correct") dayanırmı? Yalnız düzəliş orijinaldan uzun olanda (əlavə vergül,
    /// əlavə söz) məna kəsb edir.
    /// </summary>
    private static bool IsCorrectedTextAt(string text, int index, string correct, string wrong) =>
        !string.IsNullOrEmpty(correct)
        && correct.Length > wrong.Length
        && index + correct.Length <= text.Length
        && string.CompareOrdinal(text, index, correct, 0, correct.Length) == 0;

    private static int MarkupLength(EssayMistakeDto mistake) =>
        "<b></b> ()".Length + mistake.Wrong.Length + mistake.Correct.Length;

    private static string InsertMarkupAt(string text, int index, int length, EssayMistakeDto mistake) =>
        string.Concat(
            text.AsSpan(0, index),
            $"<b>{mistake.Wrong}</b> ({mistake.Correct})",
            text.AsSpan(index + length));

    /// <summary>
    /// Verilən mətnin, mövcud işarələmələrin (həm &lt;b&gt;…&lt;/b&gt;, həm ondan sonrakı
    /// mötərizəli düzəliş) HEÇ BİRİNİN içinə düşməyən ilk yerini tapır.
    /// </summary>
    private static int FindUnmarkedOccurrence(string text, string value, int startFrom)
    {
        if (string.IsNullOrEmpty(value) || startFrom < 0 || startFrom > text.Length - value.Length)
            return -1;

        var markedRanges = GetMarkedRanges(text);
        var searchStart = startFrom;

        while (searchStart <= text.Length - value.Length)
        {
            var index = text.IndexOf(value, searchStart, StringComparison.Ordinal);
            if (index < 0)
                return -1;

            var end = index + value.Length;
            var overlapsExistingMarkup = markedRanges.Any(r => index < r.End && end > r.Start);

            if (!overlapsExistingMarkup)
                return index;

            searchStart = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Artıq işarələnmiş sahələr: tam "&lt;b&gt;səhv&lt;/b&gt; (düzəliş)" cütləri, həmçinin
    /// mötərizəsiz qalmış &lt;b&gt;…&lt;/b&gt; blokları. Bu sahələrin içində yeni işarə qoyulmur.
    /// </summary>
    private static List<(int Start, int End)> GetMarkedRanges(string text)
    {
        var ranges = new List<(int Start, int End)>();

        foreach (Match match in MarkupPairPattern.Matches(text))
            ranges.Add((match.Index, match.Index + match.Length));

        foreach (Match match in BoldSpanPattern.Matches(text))
        {
            var alreadyCovered = ranges.Any(r => match.Index >= r.Start && match.Index < r.End);
            if (!alreadyCovered)
                ranges.Add((match.Index, match.Index + match.Length));
        }

        return ranges;
    }
}
