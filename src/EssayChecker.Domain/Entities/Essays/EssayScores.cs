namespace EssayChecker.Domain.Entities.Essays;

/// <summary>DİM meyarlarına görə ballar (Ümumi maks 5).</summary>
public class EssayScores
{
    public double Structure { get; set; }
    public double Content { get; set; }
    public double Grammar { get; set; }
    public double Vocabulary { get; set; }
    public double Total { get; set; }
}
