using System.Text;
using EssayChecker.Application.DTOs.Essays;

namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>Bir səhvin orijinal essedəki konkret mövqeyi.</summary>
internal readonly record struct MistakeSpan(int Start, int Length, EssayMistakeDto Mistake);

internal static class MistakeSpans
{
    /// <summary>
    /// Mövqeləri mətndəki sıraya düzür və üst-üstə düşənləri atır — eyni sahəni iki səhv tuta
    /// bilməz. Model bunu müntəzəm edir: eyni vergül üçün həm ""it is very useful but people"",
    /// həm də ""useful but people"" qaytarır, ya da kod qaydasının tutduğu ""Because""-u öz, daha
    /// geniş bir elementinin içinə salır. Belə halda uzun (daha çox kontekst verən) element qalır,
    /// qalanları həm işarələmədən, HƏM DƏ səhv siyahısından çıxarılır — əks halda şagird eyni
    /// səhvi iki dəfə, bəzən bir-birinə zidd iki düzəlişlə görürdü.
    /// </summary>
    public static List<MistakeSpan> ResolveOverlaps(IEnumerable<MistakeSpan> spans, int textLength)
    {
        var resolved = new List<MistakeSpan>();
        var cursor = 0;

        var ordered = spans
            .Where(s => s.Start >= 0 && s.Length > 0 && s.Start + s.Length <= textLength)
            .OrderBy(s => s.Start)
            .ThenByDescending(s => s.Length);

        foreach (var span in ordered)
        {
            if (span.Start < cursor)
                continue;

            resolved.Add(span);
            cursor = span.Start + span.Length;
        }

        return resolved;
    }
}

/// <summary>
/// correctedEssay-i ORİJİNAL esse mətnindən deterministik qurur. Əvvəllər bu mətni AI qaytarırdı
/// və ayrıca bir sinif onu sonradan yamayırdı; indi AI bu sahəni ümumiyyətlə yazmır. Nəticə:
/// model bütün esseni çıxışa təkrar yazmır (çıxış tokeni ~2 dəfə azalır, latency düşür) və
/// modelin düzəlişi sükutla mətnə tətbiq etməsi ("reverse coverage" pozuntusu) struktur olaraq
/// qeyri-mümkün olur — çıxış tərifə görə orijinaldan qurulur.
/// </summary>
internal static class CorrectedEssayBuilder
{
    /// <param name="spans">
    /// Hər səhvin dəqiq mövqeyi (bax <see cref="EssayEvaluationMapper"/>). Mövqe axtarışla deyil,
    /// səhvin mənbəyi tərəfindən verilir: AI elementləri unikal substring olduğu üçün tək mövqeyə
    /// malikdir, qayda əsaslı elementlərin mövqeləri isə qaydanın özündən gəlir — beləliklə
    /// "However" ifadəsinin artıq vergüllü (düzgün) nüsxəsi yanlışlıqla işarələnmir.
    /// Mapper bu siyahını artıq <see cref="MistakeSpans.ResolveOverlaps"/>-dan keçirir; burada
    /// təkrar çağırılması idempotentdir və metodu təkbaşına da düzgün saxlayır.
    /// </param>
    public static string Build(string originalEssay, IReadOnlyList<MistakeSpan> spans)
    {
        if (string.IsNullOrEmpty(originalEssay) || spans.Count == 0)
            return originalEssay;

        var ordered = MistakeSpans.ResolveOverlaps(spans, originalEssay.Length);

        var builder = new StringBuilder(originalEssay.Length + 64 * ordered.Count);
        var cursor = 0;

        foreach (var (start, length, mistake) in ordered)
        {
            builder.Append(originalEssay, cursor, start - cursor)
                   .Append("<b>").Append(originalEssay, start, length).Append("</b> (")
                   .Append(mistake.Correct).Append(')');
            cursor = start + length;
        }

        builder.Append(originalEssay, cursor, originalEssay.Length - cursor);
        return builder.ToString();
    }
}
