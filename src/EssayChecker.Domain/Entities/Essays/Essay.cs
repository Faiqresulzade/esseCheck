using EssayChecker.Domain.Enums;

namespace EssayChecker.Domain.Entities.Essays;

/// <summary>Bir esse qiymətləndirmə nəticəsi (tarixçə qeydi).</summary>
public class Essay
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    /// <summary>İstifadəçinin göndərdiyi (və ya şəkildən oxunub düzəldilmiş) orijinal mətn.</summary>
    public string OriginalText { get; set; } = null!;

    /// <summary>AI-ın düzəltdiyi esse (&lt;b&gt; teqləri ilə vurğulanmış).</summary>
    public string CorrectedEssay { get; set; } = null!;

    public int WordCount { get; set; }

    /// <summary>Dəqiqlik faizi (Ümumi bal / 5 * 100).</summary>
    public int AccuracyPercent { get; set; }

    /// <summary>Ümumi bal (0–5).</summary>
    public double TotalScore { get; set; }

    public EssayInputSource InputSource { get; set; }

    public DateTime CreatedAt { get; set; }

    public EssayStatistics Statistics { get; set; } = new();

    public EssayScores Scores { get; set; } = new();

    public TeacherFeedback Feedback { get; set; } = new();

    public List<EssayMistake> Mistakes { get; set; } = new();
}
