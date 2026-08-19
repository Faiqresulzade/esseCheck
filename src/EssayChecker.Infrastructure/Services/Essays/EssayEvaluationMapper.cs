using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.Ai;

namespace EssayChecker.Infrastructure.Services.Essays;

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

    public static EssayEvaluationData Map(AiEssayResponse dto, GradeLevel grade, string essayText)
    {
        if (string.Equals(dto.Status, "invalid", StringComparison.OrdinalIgnoreCase))
        {
            return new EssayEvaluationData
            {
                IsEssay = false,
                InvalidReason = dto.Reason ?? "The submitted text is not an essay."
            };
        }

        var mistakes = MapMistakes(dto.Mistakes);
        mistakes = AddIntroductoryCommaMistakes(mistakes, essayText);
        mistakes = AddSentenceInitialBecauseMistakes(mistakes, essayText);
        var scores = ApplyShortEssayContentFloor(MapScores(dto.Scores), grade, essayText);

        return new EssayEvaluationData
        {
            IsEssay = true,
            CorrectedEssay = CorrectedEssayMarkup.Normalize(dto.CorrectedEssay ?? string.Empty, mistakes),
            Statistics = ComputeStatistics(mistakes),
            Scores = scores,
            Mistakes = mistakes,
            Feedback = MapFeedback(dto.TeacherFeedback)
        };
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

    /// <summary>
    /// AI-ın öz-özünə saydığı "statistics" tez-tez mistakes massivinin faktiki tərkibi ilə uyğun
    /// gəlmir (bir neçə model üzərində test edilib, hamısında rast gəlindi) — ona görə etibar
    /// etmək əvəzinə, statistikanı birbaşa artıq map olunmuş mistakes siyahısından sayırıq.
    /// </summary>
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

        // AI-ın öz cəmlədiyi "total" tez-tez 4 alt-balın həqiqi cəmi ilə uyğun gəlmir (eynilə
        // statistics sayğacındakı problem kimi) — ona görə etibar etmək əvəzinə özümüz cəmləyirik.
        var total = Math.Round(s.Structure + s.Content + s.Grammar + s.Vocabulary, 1, MidpointRounding.AwayFromZero);

        return new EssayScoresDto(
            s.Structure, s.StructureComment ?? "",
            s.Content, s.ContentComment ?? "",
            s.Grammar, s.GrammarComment ?? "",
            s.Vocabulary, s.VocabularyComment ?? "",
            total);
    }

    private static IReadOnlyList<EssayMistakeDto> MapMistakes(List<AiMistake>? mistakes)
    {
        if (mistakes is null || mistakes.Count == 0)
            return Array.Empty<EssayMistakeDto>();

        var result = new List<EssayMistakeDto>(mistakes.Count);
        foreach (var m in mistakes)
        {
            var wrong = m.Wrong ?? string.Empty;
            var correct = m.Correct ?? string.Empty;

            // Promptun Section 5-i (self-check) AI-a "wrong" == "correct" olan elementləri
            // heç vaxt daxil etməməyi əmr edir, amma model bəzən buna əməl etmir (istifadəçi
            // production-da "reasons (reasons)" kimi hallar tapıb) — ona görə bunu AI-a etibar
            // etmədən burada məcburi süzürük, eynilə digər "AI-a etibar etmə" qaydaları kimi.
            if (string.Equals(wrong.Trim(), correct.Trim(), StringComparison.Ordinal))
                continue;

            var category = Enum.TryParse<MistakeCategory>(m.Category, ignoreCase: true, out var parsed)
                ? parsed
                : MistakeCategory.Grammar;

            result.Add(new EssayMistakeDto(wrong, correct, category, m.Reason ?? string.Empty));
        }

        return result;
    }

    /// <summary>
    /// Cümlə əvvəlindəki keçid ifadəsindən sonra vergül qaydası (əvvəllər prompt-un P1 qaydası)
    /// AI-a etibar edilmədən, birbaşa orijinal mətndə deterministik yoxlanılır. AI bu qaydanı
    /// mistakes massivinə əlavə etməyi tez-tez unudurdu — indi bu, kodun öz məsuliyyətidir.
    /// </summary>
    private static IReadOnlyList<EssayMistakeDto> AddIntroductoryCommaMistakes(
        IReadOnlyList<EssayMistakeDto> mistakes, string essayText)
    {
        var violations = IntroductoryCommaRule.FindViolations(essayText);
        if (violations.Count == 0)
            return mistakes;

        var existingWrongTexts = new HashSet<string>(
            mistakes.Select(m => m.Wrong.Trim()), StringComparer.Ordinal);

        var newEntries = violations
            .Select(v => v.Phrase)
            .Distinct(StringComparer.Ordinal)
            .Where(phrase => !existingWrongTexts.Contains(phrase))
            .Select(phrase => new EssayMistakeDto(
                phrase,
                phrase + ",",
                MistakeCategory.Grammar,
                "Cümlə əvvəlindəki keçid ifadəsindən sonra vergül tələb olunur."))
            .ToList();

        if (newEntries.Count == 0)
            return mistakes;

        // Section 4 tələbinə uyğun olaraq, bütün siyahını mətndə ilk görünmə sırasına görə düzürük.
        return mistakes
            .Concat(newEntries)
            .OrderBy(m =>
            {
                var idx = essayText.IndexOf(m.Wrong, StringComparison.Ordinal);
                return idx >= 0 ? idx : int.MaxValue;
            })
            .ToList();
    }

    /// <summary>
    /// Cümlə "Because" ilə başlayanda (əvvəllər AI-ın öz mühakiməsi ilə tutduğu, tutarlılığı
    /// sabit olmayan bir hal) artıq AI-a etibar edilmir — deterministik aşkarlanır və kodda
    /// təsadüfi seçilmiş sinonimlə ("As" / "Since" / "This is because") əvəz olunur.
    /// </summary>
    private static IReadOnlyList<EssayMistakeDto> AddSentenceInitialBecauseMistakes(
        IReadOnlyList<EssayMistakeDto> mistakes, string essayText)
    {
        var violations = SentenceInitialBecauseRule.FindViolations(essayText);
        if (violations.Count == 0)
            return mistakes;

        var existingWrongTexts = new HashSet<string>(
            mistakes.Select(m => m.Wrong.Trim()), StringComparer.Ordinal);

        var random = Random.Shared;
        var newEntries = new List<EssayMistakeDto>();

        foreach (var wrong in violations.Select(v => v.Wrong).Distinct(StringComparer.Ordinal))
        {
            if (existingWrongTexts.Contains(wrong))
                continue;

            var correct = SentenceInitialBecauseRule.PickSynonym(wrong, random);
            newEntries.Add(new EssayMistakeDto(
                wrong,
                correct,
                MistakeCategory.Grammar,
                "Cümlə \"Because\" ilə başlamamalıdır — sinonimlə əvəz olunur."));
        }

        if (newEntries.Count == 0)
            return mistakes;

        return mistakes
            .Concat(newEntries)
            .OrderBy(m =>
            {
                var idx = essayText.IndexOf(m.Wrong, StringComparison.Ordinal);
                return idx >= 0 ? idx : int.MaxValue;
            })
            .ToList();
    }

    private static TeacherFeedbackDto MapFeedback(AiFeedback? f) =>
        f is null
            ? new TeacherFeedbackDto(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>())
            : new TeacherFeedbackDto(
                f.Strengths ?? new List<string>(),
                f.Weaknesses ?? new List<string>(),
                f.Recommendations ?? new List<string>());
}
