namespace EssayChecker.Domain.Entities.Essays;

/// <summary>Səhv statistikası (kateqoriyalar üzrə sayı).</summary>
public class EssayStatistics
{
    public int Grammar { get; set; }
    public int Spelling { get; set; }
    public int Vocabulary { get; set; }
    public int NaturalExpression { get; set; }
    public int Total { get; set; }
}
