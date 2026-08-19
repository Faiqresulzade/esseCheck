using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.Ai;

namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>Səhv siyahısı və hər elementin orijinal mətndəki mövqeləri.</summary>
internal sealed record MistakeSet(
    IReadOnlyList<EssayMistakeDto> Mistakes,
    IReadOnlyList<MistakeSpan> Spans)
{
    public static readonly MistakeSet Empty =
        new(Array.Empty<EssayMistakeDto>(), Array.Empty<MistakeSpan>());
}

/// <summary>
/// AI-ın xam cavabını domen nəticəsinə çevirir. Burada bir prinsip hakimdir: AI-ın öz-özünə
/// hesabladığı hər şeyə (statistika sayğacları, balların cəmi, minimum söz sayı qaydası)
/// ETİBAR EDİLMİR — bunlar deterministik şəkildə yenidən hesablanır, çünki modellər bu
/// hesablamalarda müntəzəm səhv edir (bir neçə model üzərində təsdiqlənib).
/// </summary>
internal static class EssayEvaluationMapper
{
    /// <summary>11-ci sinifdə bu həddən az söz varsa, content balı istisnasız 0 olmalıdır (AI-a etibar edilmir).</summary>
    private const int Grade11ContentZeroFloorWords = 70;

    /// <summary>Promptdakı hədd ilə eyni — model onu aşsa da, siyahı burada kəsilir.</summary>
    private const int MaxMistakes = 20;

    /// <summary>
    /// ÇAĞIRIŞ A-nın nəticəsini süzür və üzərinə deterministik qayda əsaslı səhvləri əlavə edir.
    /// Nəticə ÇAĞIRIŞ B-yə giriş kimi verilir və eyni zamanda son cavabın əsasını təşkil edir.
    /// </summary>
    public static MistakeSet BuildMistakes(List<AiMistake>? aiMistakes, string essayText)
    {
        var spans = new List<MistakeSpan>();
        AddAiMistakes(spans, aiMistakes, essayText);

        var existingWrongTexts = new HashSet<string>(
            spans.Select(s => s.Mistake.Wrong), StringComparer.Ordinal);

        AddIntroductoryCommaMistakes(spans, essayText, existingWrongTexts);
        AddSentenceInitialBecauseMistakes(spans, essayText, existingWrongTexts);

        // Üst-üstə düşən mövqelər həm işarələmədən, həm siyahıdan çıxarılır — şagirdə eyni səhv
        // iki dəfə (bəzən zidd iki düzəlişlə) göstərilməməlidir.
        var resolved = MistakeSpans.ResolveOverlaps(spans, essayText.Length);
        if (resolved.Count == 0)
            return MistakeSet.Empty;

        // Section 4 tələbinə uyğun olaraq siyahı mətndə ilk görünmə sırasındadır (ResolveOverlaps
        // artıq bu sıraya düzüb). Bir DTO-nun (qayda əsaslı elementlərdə) bir neçə mövqeyi ola
        // bilər — siyahıda yalnız bir dəfə görünür.
        var mistakes = resolved
            .Select(s => s.Mistake)
            .Distinct()
            .ToList();

        return new MistakeSet(mistakes, resolved);
    }

    public static EssayEvaluationData Map(
        MistakeSet mistakeSet,
        AiScoringResponse scoring,
        GradeLevel grade,
        string essayText)
    {
        var scores = ApplyShortEssayContentFloor(MapScores(scoring.Scores), grade, essayText);

        return new EssayEvaluationData
        {
            IsEssay = true,
            CorrectedEssay = CorrectedEssayBuilder.Build(essayText, mistakeSet.Spans),
            Statistics = ComputeStatistics(mistakeSet.Mistakes),
            Scores = scores,
            Mistakes = mistakeSet.Mistakes,
            Feedback = MapFeedback(scoring.TeacherFeedback)
        };
    }

    /// <summary>
    /// AI-ın qaytardığı hər elementi essenin özü ilə üzləşdirib süzür. Model uydurduğu və ya
    /// mövqeyi qeyri-müəyyən olan elementlər buraxılır — müəllim etibarını ən tez itirən nöqtə
    /// budur.
    /// </summary>
    private static void AddAiMistakes(List<MistakeSpan> spans, List<AiMistake>? aiMistakes, string essayText)
    {
        if (aiMistakes is null || aiMistakes.Count == 0)
            return;

        var seenWrong = new HashSet<string>(StringComparer.Ordinal);

        foreach (var m in aiMistakes)
        {
            var wrong = (m.Wrong ?? string.Empty).Trim();
            var correct = (m.Correct ?? string.Empty).Trim();

            if (wrong.Length == 0 || correct.Length == 0)
                continue;

            // Eyni mətn (yalnız hərf ölçüsü fərqi də daxil) — Section 6-ya görə capitalization
            // heç vaxt səhv deyil, ona görə belə element mənasızdır.
            if (string.Equals(wrong, correct, StringComparison.OrdinalIgnoreCase))
                continue;

            // Halüsinasiya: şagirdin yazmadığı mətn müəllimə göstərilə bilməz.
            var first = essayText.IndexOf(wrong, StringComparison.Ordinal);
            if (first < 0)
                continue;

            // Unikal olmayan "wrong" hansı nüsxəyə aid olduğunu bildirmir — işarələmə təsadüfi
            // yerə düşərdi və B1-in "ilk nüsxəyə toxunma" qaydası pozulardı.
            if (essayText.IndexOf(wrong, first + 1, StringComparison.Ordinal) >= 0)
                continue;

            if (!seenWrong.Add(wrong))
                continue;

            var category = Enum.TryParse<MistakeCategory>(m.Category, ignoreCase: true, out var parsed)
                ? parsed
                : MistakeCategory.Grammar;

            spans.Add(new MistakeSpan(first, wrong.Length,
                new EssayMistakeDto(wrong, correct, category, m.Reason ?? string.Empty)));

            if (spans.Count == MaxMistakes)
                break;
        }
    }

    /// <summary>
    /// Cümlə əvvəlindəki keçid ifadəsindən sonra vergül qaydası (əvvəllər prompt-un P1 qaydası)
    /// AI-a etibar edilmədən, birbaşa orijinal mətndə deterministik yoxlanılır. AI bu qaydanı
    /// mistakes massivinə əlavə etməyi tez-tez unudurdu — indi bu, kodun öz məsuliyyətidir.
    /// </summary>
    private static void AddIntroductoryCommaMistakes(
        List<MistakeSpan> spans, string essayText, HashSet<string> existingWrongTexts)
    {
        var byPhrase = new Dictionary<string, EssayMistakeDto>(StringComparer.Ordinal);

        foreach (var violation in IntroductoryCommaRule.FindViolations(essayText))
        {
            if (existingWrongTexts.Contains(violation.Phrase))
                continue;

            if (!byPhrase.TryGetValue(violation.Phrase, out var mistake))
            {
                mistake = new EssayMistakeDto(
                    violation.Phrase,
                    violation.Phrase + ",",
                    MistakeCategory.Grammar,
                    "Cümlə əvvəlindəki keçid ifadəsindən sonra vergül tələb olunur.");
                byPhrase.Add(violation.Phrase, mistake);
            }

            spans.Add(new MistakeSpan(violation.Index, violation.Phrase.Length, mistake));
        }
    }

    /// <summary>
    /// Cümlə "Because" ilə başlayanda (əvvəllər AI-ın öz mühakiməsi ilə tutduğu, tutarlılığı
    /// sabit olmayan bir hal) artıq AI-a etibar edilmir — deterministik aşkarlanır və kodda
    /// təsadüfi seçilmiş sinonimlə ("As" / "Since" / "This is because") əvəz olunur.
    /// </summary>
    private static void AddSentenceInitialBecauseMistakes(
        List<MistakeSpan> spans, string essayText, HashSet<string> existingWrongTexts)
    {
        var byWord = new Dictionary<string, EssayMistakeDto>(StringComparer.Ordinal);

        foreach (var violation in SentenceInitialBecauseRule.FindViolations(essayText))
        {
            if (existingWrongTexts.Contains(violation.Wrong))
                continue;

            if (!byWord.TryGetValue(violation.Wrong, out var mistake))
            {
                mistake = new EssayMistakeDto(
                    violation.Wrong,
                    SentenceInitialBecauseRule.PickSynonym(violation.Wrong, Random.Shared),
                    MistakeCategory.Grammar,
                    "Cümlə \"Because\" ilə başlamamalıdır — sinonimlə əvəz olunur.");
                byWord.Add(violation.Wrong, mistake);
            }

            spans.Add(new MistakeSpan(violation.Index, violation.Wrong.Length, mistake));
        }
    }

    /// <summary>
    /// 11-ci sinifdə 70 sözdən az essedə content balı kodla məcburi 0-a endirilir, AI nə
    /// qaytarsa qaytarsın (istənilən halda).
    /// </summary>
    private static EssayScoresDto ApplyShortEssayContentFloor(EssayScoresDto scores, GradeLevel grade, string essayText)
    {
        if (grade != GradeLevel.Grade11 || EssayPrompts.CountWords(essayText) >= Grade11ContentZeroFloorWords)
            return scores;

        return scores with
        {
            Content = 0,
            ContentComment = "70 sözdən az yazılmış esselərdə (11-ci sinif) məzmun balı avtomatik 0 təyin olunur.",
            Total = scores.Structure + 0 + scores.Grammar + scores.Vocabulary
        };
    }

    private static EssayStatisticsDto ComputeStatistics(IReadOnlyList<EssayMistakeDto> mistakes)
    {
        var grammar = 0;
        var spelling = 0;
        var vocabulary = 0;
        var naturalExpression = 0;

        foreach (var m in mistakes)
        {
            switch (m.Category)
            {
                case MistakeCategory.Grammar: grammar++; break;
                case MistakeCategory.Spelling: spelling++; break;
                case MistakeCategory.Vocabulary: vocabulary++; break;
                case MistakeCategory.NaturalExpression: naturalExpression++; break;
            }
        }

        return new EssayStatisticsDto(grammar, spelling, vocabulary, naturalExpression, mistakes.Count);
    }

    private static EssayScoresDto MapScores(AiScores? s)
    {
        if (s is null)
            return new EssayScoresDto(0, "", 0, "", 0, "", 0, "", 0);

        var total = Math.Round(s.Structure + s.Content + s.Grammar + s.Vocabulary, 1, MidpointRounding.AwayFromZero);

        return new EssayScoresDto(
            s.Structure, s.StructureComment ?? "",
            s.Content, s.ContentComment ?? "",
            s.Grammar, s.GrammarComment ?? "",
            s.Vocabulary, s.VocabularyComment ?? "",
            total);
    }

    private static TeacherFeedbackDto MapFeedback(AiFeedback? f) =>
        f is null
            ? new TeacherFeedbackDto(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>())
            : new TeacherFeedbackDto(
                f.Strengths ?? new List<string>(),
                f.Weaknesses ?? new List<string>(),
                f.Recommendations ?? new List<string>());
}
