namespace EssayChecker.Application.DTOs.Analytics;

/// <summary>
/// Bir essenin analitika üçün lazım olan sahələri. Mətn sahələri (OriginalText,
/// CorrectedEssay) qəsdən daxil deyil — yüzlərlə sətir oxunanda onlar ölçünün 99%-ni tutur.
/// </summary>
public sealed record EssayAnalyticsRow(
    int EssayId,
    int? StudentId,
    // Şagirdin qrupu — şagird seçilməyibsə (və ya silinibsə) null.
    int? GroupId,
    DateTime CreatedAt,
    string Title,
    int WordCount,
    double Total,
    double Structure,
    double Content,
    double Grammar,
    double Vocabulary,
    int MistakeTotal,
    int MistakeGrammar,
    int MistakeSpelling,
    int MistakeVocabulary,
    int MistakeNaturalExpression);

/// <summary>Bir essenin AI rəyindən yalnız zəif tərəflər və tövsiyələr.</summary>
public sealed record FeedbackRow(
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> Recommendations);
