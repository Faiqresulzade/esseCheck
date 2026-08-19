namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>
/// ÇAĞIRIŞ A-nın (səhv axtarışı) xam cavabı. Bu tiplər YALNIZ deserializasiya üçündür; domenə
/// çevrilmə və AI-a etibar edilməyən sahələrin yenidən hesablanması
/// <see cref="EssayEvaluationMapper"/>-də baş verir.
/// </summary>
internal sealed class AiDetectionResponse
{
    public bool IsEssay { get; set; } = true;
    public List<AiMistake>? Mistakes { get; set; }
}

/// <summary>ÇAĞIRIŞ B-nin (bal + müəllim rəyi) xam cavabı.</summary>
internal sealed class AiScoringResponse
{
    public AiScores? Scores { get; set; }
    public AiFeedback? TeacherFeedback { get; set; }
}

internal sealed class AiMistake
{
    public string? Wrong { get; set; }
    public string? Correct { get; set; }
    public string? Category { get; set; }
    public string? Reason { get; set; }
}

internal sealed class AiScores
{
    public double Structure { get; set; }
    public string? StructureComment { get; set; }
    public double Content { get; set; }
    public string? ContentComment { get; set; }
    public double Grammar { get; set; }
    public string? GrammarComment { get; set; }
    public double Vocabulary { get; set; }
    public string? VocabularyComment { get; set; }
}

internal sealed class AiFeedback
{
    public List<string>? Strengths { get; set; }
    public List<string>? Weaknesses { get; set; }
    public List<string>? Recommendations { get; set; }
}
