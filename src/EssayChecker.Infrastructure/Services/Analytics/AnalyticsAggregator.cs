using EssayChecker.Application.DTOs.Analytics;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Infrastructure.Services.Analytics;

/// <summary>
/// Xam esse sətirlərindən icmal rəqəmləri çıxaran saf funksiyalar (DB-yə toxunmur).
/// Şagird, qrup və ümumi panel eyni funksiyaları çağırır ki, üç ekranda eyni essenin
/// rəqəmi fərqli çıxmasın.
/// </summary>
internal static class AnalyticsAggregator
{
    /// <summary>
    /// İstiqamətlərin maksimum balları (DİM: content 2.0, qalanları 1.0 — cəmi 5.0).
    /// Müqayisə həmişə maksimuma nisbətdə aparılır, xam balla yox.
    /// </summary>
    private static readonly (EssayDirection Direction, double Max)[] Directions =
    {
        (EssayDirection.Structure, 1.0),
        (EssayDirection.Content, 2.0),
        (EssayDirection.Grammar, 1.0),
        (EssayDirection.Vocabulary, 1.0)
    };

    private const double TotalMax = 5.0;

    /// <summary>Trend qrafiki üçün ən çox neçə nöqtə qaytarılır (ən yenilər saxlanır).</summary>
    public const int MaxTrendPoints = 100;

    /// <summary>Zəif tərəf / tövsiyə üçün neçə son esse oxunur və neçə qeyd qaytarılır.</summary>
    public const int FeedbackEssayWindow = 10;
    public const int MaxHighlights = 5;

    /// <summary>Trendin mənalı sayılması üçün minimum esse sayı.</summary>
    public const int MinEssaysForTrend = 2;

    public static ScoreSummary BuildScores(IReadOnlyList<EssayAnalyticsRow> rows)
    {
        if (rows.Count == 0)
        {
            return new ScoreSummary(
                0, 0,
                Directions.Select(d => new DirectionStat(d.Direction, 0, d.Max, 0)).ToList());
        }

        var total = rows.Average(r => r.Total);

        var stats = Directions
            .Select(d =>
            {
                var average = rows.Average(r => Value(r, d.Direction));
                return new DirectionStat(
                    d.Direction,
                    Round2(average),
                    d.Max,
                    Round1(average / d.Max * 100));
            })
            .ToList();

        return new ScoreSummary(Round2(total), Round1(total / TotalMax * 100), stats);
    }

    /// <summary>Faizcə ən aşağı istiqamət. Esse yoxdursa null.</summary>
    public static EssayDirection? WeakestDirection(ScoreSummary scores, int essayCount) =>
        essayCount == 0
            ? null
            : scores.Directions.OrderBy(d => d.Percent).ThenBy(d => d.Direction).First().Direction;

    public static MistakeSummary BuildMistakes(IReadOnlyList<EssayAnalyticsRow> rows)
    {
        var total = rows.Sum(r => r.MistakeTotal);
        var words = rows.Sum(r => r.WordCount);

        var categories = new[]
        {
            (MistakeCategory.Grammar, rows.Sum(r => r.MistakeGrammar)),
            (MistakeCategory.Spelling, rows.Sum(r => r.MistakeSpelling)),
            (MistakeCategory.Vocabulary, rows.Sum(r => r.MistakeVocabulary)),
            (MistakeCategory.NaturalExpression, rows.Sum(r => r.MistakeNaturalExpression))
        };

        // Pay kateqoriyaların cəminə görə hesablanır (MistakeTotal-a görə yox) — köhnə
        // qeydlərdə Total ilə kateqoriya cəmi fərqlənsə belə paylar 100%-ə yığılsın.
        var categorySum = categories.Sum(c => c.Item2);

        return new MistakeSummary(
            total,
            rows.Count == 0 ? 0 : Round1((double)total / rows.Count),
            words == 0 ? 0 : Round1((double)total / words * 100),
            categories
                .Select(c => new MistakeCategoryStat(
                    c.Item1,
                    c.Item2,
                    categorySum == 0 ? 0 : Round1((double)c.Item2 / categorySum * 100)))
                .ToList());
    }

    /// <summary>Ən yeni <see cref="MaxTrendPoints"/> nöqtə, tarixə görə artan sırada.</summary>
    public static IReadOnlyList<ScorePoint> BuildTrend(IReadOnlyList<EssayAnalyticsRow> rows) =>
        rows
            .OrderBy(r => r.CreatedAt)
            .TakeLast(MaxTrendPoints)
            .Select(r => new ScorePoint(
                r.EssayId,
                r.CreatedAt,
                r.Title,
                r.WordCount,
                r.Total,
                r.Structure,
                r.Content,
                r.Grammar,
                r.Vocabulary,
                r.MistakeTotal))
            .ToList();

    /// <summary>Son iki essenin ümumi balı və fərqi (müsbət = irəliləyiş).</summary>
    public static (double? Latest, double? Previous, double? Delta) LatestProgress(
        IReadOnlyList<EssayAnalyticsRow> rows)
    {
        var ordered = rows.OrderBy(r => r.CreatedAt).ToList();
        if (ordered.Count == 0)
            return (null, null, null);

        var latest = ordered[^1].Total;
        if (ordered.Count == 1)
            return (Round2(latest), null, null);

        var previous = ordered[^2].Total;
        return (Round2(latest), Round2(previous), Round2(latest - previous));
    }

    /// <summary>
    /// AI-ın yazdığı qeydləri təkrarlanma sayına görə sıralayır. Mətn AI-dan gəldiyi üçün
    /// eyni fikir fərqli sözlərlə yazıla bilər — burada yalnız mətn səviyyəsində (boşluq/registr
    /// normallaşdırılmaqla) eyni olanlar birləşdirilir, semantik qruplaşdırma edilmir.
    /// </summary>
    public static IReadOnlyList<FeedbackHighlight> BuildHighlights(IEnumerable<string> texts)
    {
        return texts
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => (Original: t.Trim(), Key: Normalize(t)))
            .GroupBy(t => t.Key)
            .Select(g => new FeedbackHighlight(g.First().Original, g.Count()))
            .OrderByDescending(h => h.Count)
            .ThenBy(h => h.Text, StringComparer.OrdinalIgnoreCase)
            .Take(MaxHighlights)
            .ToList();
    }

    private static string Normalize(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant()
            .TrimEnd('.', '!', '?', ' ');

    private static double Value(EssayAnalyticsRow row, EssayDirection direction) => direction switch
    {
        EssayDirection.Structure => row.Structure,
        EssayDirection.Content => row.Content,
        EssayDirection.Grammar => row.Grammar,
        _ => row.Vocabulary
    };

    private static double Round1(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static double Round2(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
