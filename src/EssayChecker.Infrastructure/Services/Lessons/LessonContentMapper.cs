using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Infrastructure.Services.Lessons;

/// <summary>
/// AI cavabını DTO-lara çevirir. Qəsdən MİNİMAL müdaxilə edir:
/// - Slayd və sual SAYINA toxunulmur — AI 5 slayd verirsə 5 qaytarılır, boş slayd uydurulmur.
/// - Bütün sahələr zəmanətlə mövcud olur (null / boş massiv), çünki frontend sənədi §3.1 bunu
///   tələb edir və bu, məzmunu dəyişmək deyil, sadəcə forma zəmanətidir.
/// - Yeganə atılan element: <c>correctIndex</c>-i variant siyahısından kənarda olan test sualı.
///   Belə sual şagirdə göstərilsə, düzgün cavabı "səhv" kimi qeyd edər — 2 suallı test 1 sınıq
///   sualdan yaxşıdır.
/// </summary>
internal static class LessonContentMapper
{
    public static IReadOnlyList<LessonSlideDto> MapSlides(List<AiLessonSlide>? slides) =>
        slides is null
            ? Array.Empty<LessonSlideDto>()
            : slides.Select(MapSlide).ToList();

    private static LessonSlideDto MapSlide(AiLessonSlide s)
    {
        var keywords = Clean(s.Keywords);
        var points = Clean(s.Points);

        var examples = (s.Examples ?? new List<AiLessonExample>())
            .Where(e => !string.IsNullOrWhiteSpace(e.En))
            .Select(e => new LessonExampleDto(e.En!.Trim(), e.Az?.Trim() ?? string.Empty, Blank(e.Highlight)))
            .ToList();

        var mistakes = (s.Mistakes ?? new List<AiLessonMistake>())
            .Where(m => !string.IsNullOrWhiteSpace(m.Wrong) && !string.IsNullOrWhiteSpace(m.Correct))
            .Select(m => new LessonMistakeDto(m.Wrong!.Trim(), m.Correct!.Trim(), m.Note?.Trim() ?? string.Empty))
            .ToList();

        var comparison = s.Comparison is null || string.IsNullOrWhiteSpace(s.Comparison.LeftTitle)
            ? null
            : new LessonComparisonDto(
                s.Comparison.LeftTitle!.Trim(),
                s.Comparison.LeftBody?.Trim() ?? string.Empty,
                s.Comparison.RightTitle?.Trim() ?? string.Empty,
                s.Comparison.RightBody?.Trim() ?? string.Empty);

        return new LessonSlideDto(
            ResolveType(s, examples.Count, mistakes.Count, comparison, points.Count),
            s.Title?.Trim() ?? string.Empty,
            Blank(s.Body),
            Blank(s.Formula),
            keywords,
            examples,
            mistakes,
            comparison,
            points);
    }

    /// <summary>
    /// Slayd növü. Sxem enum-la məhdudlaşdırır, amma ehtiyat model struktur çıxışı
    /// dəstəkləməyə bilər — o halda növ məzmundan çıxarılır (uydurulmur, sadəcə oxunur).
    /// </summary>
    private static LessonSlideType ResolveType(
        AiLessonSlide slide, int exampleCount, int mistakeCount, LessonComparisonDto? comparison, int pointCount)
    {
        if (Enum.TryParse<LessonSlideType>(slide.Type, ignoreCase: true, out var parsed))
            return parsed;

        if (exampleCount > 0) return LessonSlideType.Examples;
        if (mistakeCount > 0) return LessonSlideType.Mistakes;
        if (comparison is not null) return LessonSlideType.Compare;
        if (pointCount > 0) return LessonSlideType.Summary;
        return string.IsNullOrWhiteSpace(slide.Formula) ? LessonSlideType.Intro : LessonSlideType.Rule;
    }

    public static IReadOnlyList<LessonQuizQuestionDto> MapQuiz(List<AiQuizQuestion>? quiz)
    {
        if (quiz is null)
            return Array.Empty<LessonQuizQuestionDto>();

        var result = new List<LessonQuizQuestionDto>(quiz.Count);

        // Sürüşdürmənin başlanğıcı bütün test üçün bir dəfə, ilk qəbul olunan sualın mətnindən
        // hesablanır — beləliklə hədəf mövqelər sual-sual ardıcıl gedir.
        int? seed = null;

        foreach (var q in quiz)
        {
            if (string.IsNullOrWhiteSpace(q.Question))
                continue;

            var options = Clean(q.Options);

            // Düzgün cavabın indeksi variant siyahısından kənardadırsa sual yararsızdır.
            if (q.CorrectIndex < 0 || q.CorrectIndex >= options.Count)
                continue;

            var question = q.Question.Trim();
            var (rotated, correctIndex) = SpreadCorrectAnswer(options, q.CorrectIndex, result.Count, seed ??= StableHash(question));

            result.Add(new LessonQuizQuestionDto(
                question,
                rotated,
                correctIndex,
                q.Explanation?.Trim() ?? string.Empty));
        }

        return result;
    }

    /// <summary>
    /// Variantları dövri sürüşdürür ki, düzgün cavab hər sualda başqa mövqedə olsun.
    ///
    /// Səbəb: model promptdakı "düzgün cavabın yerini dəyişdir" tələbini davamlı olaraq gözardı
    /// edir — ölçmədə hər iki test dərsində üç sualın da cavabı 0-cı variantda idi. Şagird bunu
    /// bir neçə dərsdən sonra öyrənir və test mənasını itirir.
    ///
    /// Bu, məzmuna müdaxilə deyil: variantlar sıralı siyahı deyil, model onları hər hansı ardıcıllıqla
    /// verir. Sürüşdürmə mətnin özünə toxunmur, yalnız mövqeyi dəyişir və <c>correctIndex</c> ona
    /// uyğun köçürülür.
    ///
    /// Deterministikdir: sürüşdürmə məbləği sualın mətnindən çıxarılan sabit heşdən + sualın
    /// sırasından asılıdır. Yəni eyni dərs həmişə eyni ardıcıllığı verir — keşdəki şablon və ondan
    /// kopyalanmış dərslər bir-birindən fərqlənə bilməz.
    /// </summary>
    private static (IReadOnlyList<string> Options, int CorrectIndex) SpreadCorrectAnswer(
        IReadOnlyList<string> options, int correctIndex, int questionIndex, int seed)
    {
        var n = options.Count;

        // "All of the above" tipli variantlar mövqeyə bağlıdır — onları yerindən tərpətmək olmaz.
        if (n < 2 || options.Any(IsPositionDependent))
            return (options, correctIndex);

        // Ardıcıl suallar üçün hədəf mövqe də ardıcıl seçilir — beləliklə üç sual üç fərqli
        // mövqeyə düşür (n >= 3 olduqda zəmanətlə).
        var target = (seed + questionIndex) % n;
        var shift = (target - correctIndex + n) % n;

        if (shift == 0)
            return (options, correctIndex);

        var rotated = new string[n];
        for (var i = 0; i < n; i++)
            rotated[(i + shift) % n] = options[i];

        return (rotated, target);
    }

    private static bool IsPositionDependent(string option) =>
        option.Contains("above", StringComparison.OrdinalIgnoreCase) ||
        option.Contains("yuxarıdakı", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Prosesdən asılı olmayan sabit heş (FNV-1a). <c>string.GetHashCode</c> hər proses başlanğıcında
    /// fərqli nəticə verir, ona görə burada işlədilə bilməz — nəticə deterministik olmalıdır.
    /// </summary>
    private static int StableHash(string value)
    {
        unchecked
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            var hash = offsetBasis;
            foreach (var c in value)
            {
                hash ^= c;
                hash *= prime;
            }

            return (int)(hash & 0x7FFFFFFF);
        }
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyList<string> Clean(List<string>? values) =>
        values is null
            ? Array.Empty<string>()
            : values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList();
}
