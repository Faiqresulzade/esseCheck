namespace EssayChecker.Infrastructure.Services.Essays;

/// <summary>
/// AI-ın qaytardığı xam JSON-un birbaşa əksi (bax EssayPrompts Section 14 — "REQUIRED OUTPUT
/// SHAPE"). Bu tiplər YALNIZ deserializasiya üçündür; domen tərəfə çevrilmə və AI-a etibar
/// edilməyən sahələrin yenidən hesablanması <see cref="EssayEvaluationMapper"/>-də baş verir.
///
/// Qeyd: AI "statistics" obyekti də qaytarır, amma biz onu oxumuruq — sayğaclar mistakes
/// massivindən özümüz hesablanır (bax mapper). System.Text.Json tanımadığı JSON sahələrini
/// sükutla nəzərə almadığı üçün burada həmin sahə ümumiyyətlə elan edilmir.
/// </summary>
internal sealed class AiEssayResponse
{
    public string? Status { get; set; }
    public string? Reason { get; set; }
    public string? CorrectedEssay { get; set; }
    public List<AiMistake>? Mistakes { get; set; }
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

    /// <summary>AI-ın öz cəmlədiyi bal — etibarsızdır, mapper onu yenidən hesablayır.</summary>
    public double Total { get; set; }
}

internal sealed class AiFeedback
{
    public List<string>? Strengths { get; set; }
    public List<string>? Weaknesses { get; set; }
    public List<string>? Recommendations { get; set; }
}
