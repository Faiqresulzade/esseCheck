using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Analytics;

/// <summary>DİM-in 4 qiymətləndirmə istiqaməti (bax EssayPrompts.ScoringRules §10).</summary>
public enum EssayDirection
{
    Structure = 0,
    Content = 1,
    Grammar = 2,
    Vocabulary = 3
}

/// <summary>
/// Bir istiqamətin orta balı. İstiqamətlərin maksimumu fərqlidir (content 2.0, qalanları 1.0),
/// ona görə müqayisə və qrafik üçün xam bal yox, Percent istifadə olunmalıdır.
/// </summary>
public sealed record DirectionStat(EssayDirection Direction, double Average, double Max, double Percent);

/// <summary>Ballar icmalı: ümumi bal (0–5) + 4 istiqamət.</summary>
public sealed record ScoreSummary(
    double Total,
    double TotalPercent,
    IReadOnlyList<DirectionStat> Directions);

/// <summary>Kateqoriya üzrə səhv sayı və ümumi səhvlərdəki payı (%).</summary>
public sealed record MistakeCategoryStat(MistakeCategory Category, int Count, double Share);

/// <summary>
/// Səhv profili. PerHundredWords esse uzunluğundan asılı olmayan müqayisə üçündür —
/// uzun esse təbii olaraq daha çox səhv verir, ona görə xam say aldadıcıdır.
/// </summary>
public sealed record MistakeSummary(
    int Total,
    double AveragePerEssay,
    double PerHundredWords,
    IReadOnlyList<MistakeCategoryStat> Categories);

/// <summary>Trend qrafiki üçün bir nöqtə (bir esse). Tarixə görə artan sırada gəlir.</summary>
public sealed record ScorePoint(
    int EssayId,
    DateTime Date,
    string Title,
    int WordCount,
    double Total,
    double Structure,
    double Content,
    double Grammar,
    double Vocabulary,
    int MistakeCount);

/// <summary>
/// AI-ın esse rəyində yazdığı zəif tərəf / tövsiyə qeydi. Count = son esselərdə neçə dəfə
/// (mətn səviyyəsində) təkrarlanıb — çox təkrarlanan qeyd davamlı zəif tərəfdir.
/// </summary>
public sealed record FeedbackHighlight(string Text, int Count);

/// <summary>
/// Şagird profili: trend + zəif tərəflər. HasEnoughData = ən azı 2 esse var (az olduqda
/// trendi qrafik kimi göstərmə, yalnız orta balı göstər).
/// </summary>
public sealed record StudentAnalyticsResponse(
    int StudentId,
    string FullName,
    int GroupId,
    string GroupName,
    GradeLevel? Grade,
    int EssayCount,
    bool HasEnoughData,
    ScoreSummary Scores,
    EssayDirection? WeakestDirection,
    double? LatestTotal,
    double? PreviousTotal,
    double? Delta,
    MistakeSummary Mistakes,
    IReadOnlyList<ScorePoint> Trend,
    IReadOnlyList<FeedbackHighlight> Weaknesses,
    IReadOnlyList<FeedbackHighlight> Recommendations);

/// <summary>
/// Qrup icmalındakı bir şagird sətri (leaderboard). Rank orta bala görədir (1 = ən yüksək);
/// essesi olmayan şagirdlər sonda gəlir və Rank = 0 olur.
/// </summary>
public sealed record GroupStudentSummary(
    int StudentId,
    string FullName,
    int Rank,
    int EssayCount,
    double? AverageTotal,
    double? LatestTotal,
    double? Delta,
    EssayDirection? WeakestDirection);

/// <summary>Qrup icmalı: sinifin ümumi vəziyyəti + şagird sıralaması.</summary>
public sealed record GroupAnalyticsResponse(
    int GroupId,
    string Name,
    int StudentCount,
    int EssayCount,
    bool HasEnoughData,
    ScoreSummary Scores,
    EssayDirection? WeakestDirection,
    MistakeSummary Mistakes,
    IReadOnlyList<GroupStudentSummary> Students);

/// <summary>Ümumi paneldəki qrup sətri.</summary>
public sealed record GroupSummary(
    int GroupId,
    string Name,
    int StudentCount,
    int EssayCount,
    double? AverageTotal);

/// <summary>
/// Müəllimin ümumi paneli — bütün esseləri, şagird seçilməyənlər də daxil.
/// EssaysWithStudent = şagirdə bağlanmış esselərin sayı (qalanı müəllimin öz esseləridir).
/// </summary>
public sealed record OverviewAnalyticsResponse(
    int GroupCount,
    int StudentCount,
    int EssayCount,
    int EssaysWithStudent,
    int EssaysLast30Days,
    bool HasEnoughData,
    ScoreSummary Scores,
    EssayDirection? WeakestDirection,
    MistakeSummary Mistakes,
    IReadOnlyList<FeedbackHighlight> Weaknesses,
    IReadOnlyList<FeedbackHighlight> Recommendations,
    IReadOnlyList<GroupSummary> Groups);
