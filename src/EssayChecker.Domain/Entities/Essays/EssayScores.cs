namespace EssayChecker.Domain.Entities.Essays;

/// <summary>DİM meyarlarına görə ballar (Ümumi maks 5).</summary>
public class EssayScores
{
    public double Structure { get; set; }
    public string StructureComment { get; set; } = string.Empty;

    public double Content { get; set; }
    public string ContentComment { get; set; } = string.Empty;

    public double Grammar { get; set; }
    public string GrammarComment { get; set; } = string.Empty;

    public double Vocabulary { get; set; }
    public string VocabularyComment { get; set; } = string.Empty;

    public double Total { get; set; }
}
