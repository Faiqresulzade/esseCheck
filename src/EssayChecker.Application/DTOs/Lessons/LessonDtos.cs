using System.ComponentModel.DataAnnotations;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Lessons;

public sealed class CreateLessonRequest
{
    [Required(ErrorMessage = "Mövzu tələb olunur.")]
    [MaxLength(200, ErrorMessage = "Mövzu 200 simvoldan uzun ola bilməz.")]
    public string Topic { get; set; } = null!;

    /// <summary>
    /// Opsional: göndərilməyibsə və <see cref="StudentId"/> verilibsə şagirdin kartındakı sinif
    /// işlədilir (esse endpoint-indəki eyni məntiq).
    /// </summary>
    [EnumDataType(typeof(GradeLevel), ErrorMessage = "Sinif yalnız Grade9 və ya Grade11 ola bilər.")]
    public GradeLevel? Grade { get; set; }

    /// <summary>Opsional — dərs hansı şagird üçün qeyd olunsun (yalnız etiket/filtr).</summary>
    public int? StudentId { get; set; }
}

/// <summary>
/// Nümunə cümlə. <see cref="En"/> tətbiqdə səsləndirilir, <see cref="Highlight"/> isə onun
/// vurğulanacaq alt-sətridir (tapılmasa frontend sadəcə vurğulamır).
/// </summary>
public sealed record LessonExampleDto(string En, string Az, string? Highlight);

public sealed record LessonMistakeDto(string Wrong, string Correct, string Note);

public sealed record LessonComparisonDto(string LeftTitle, string LeftBody, string RightTitle, string RightBody);

/// <summary>
/// Bir slayd. İstifadə olunmayan sahələr də HƏMİŞƏ qaytarılır (null / boş massiv) — frontend
/// "sahə yoxdur" halını yoxlamasın deyə.
/// </summary>
public sealed record LessonSlideDto(
    LessonSlideType Type,
    string Title,
    string? Body,
    string? Formula,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<LessonExampleDto> Examples,
    IReadOnlyList<LessonMistakeDto> Mistakes,
    LessonComparisonDto? Comparison,
    IReadOnlyList<string> Points);

/// <summary>Mini test sualı — 4 variant, <see cref="CorrectIndex"/> 0-3 aralığındadır.</summary>
public sealed record LessonQuizQuestionDto(
    string Question,
    IReadOnlyList<string> Options,
    int CorrectIndex,
    string Explanation);

public sealed record LessonResponse(
    int Id,
    string Topic,
    GradeLevel Grade,
    int? StudentId,
    string? StudentName,
    DateTime CreatedAt,
    IReadOnlyList<LessonSlideDto> Slides,
    IReadOnlyList<LessonQuizQuestionDto> Quiz);

/// <summary>Siyahı sətri — slaydların məzmunu daxil deyil, yalnız <see cref="SlideCount"/>.</summary>
public sealed record LessonListItemResponse(
    int Id,
    string Topic,
    GradeLevel Grade,
    int? StudentId,
    string? StudentName,
    int SlideCount,
    DateTime CreatedAt);

public sealed record LessonHistoryResponse(
    IReadOnlyList<LessonListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public enum CreateLessonOutcome
{
    /// <summary>Yeni dərs yaradıldı (AI və ya keşdən) — gündəlik sayğac artırıldı.</summary>
    Created = 0,

    /// <summary>İstifadəçinin siyahısında bu mövzu artıq var idi — hazır dərs açıldı, limit toxunulmadı.</summary>
    Reused = 1,

    /// <summary>Mövzu İngilis dili dərsinə aid deyil — heç nə saxlanılmadı, limit toxunulmadı.</summary>
    InvalidTopic = 2,

    /// <summary>Gündəlik dərs limiti bitib.</summary>
    LimitExceeded = 3
}

/// <summary>
/// Dərs yaradılışının nəticəsi. Limit qərarı qəsdən servisin içindədir (esse axınından fərqli
/// olaraq): mövcud dərsin təkrar açılması limit xərcləmir, bunu isə yalnız servis bilir.
/// </summary>
public sealed record CreateLessonResult(CreateLessonOutcome Outcome, string? Error, LessonResponse? Lesson)
{
    public static CreateLessonResult Created(LessonResponse lesson) => new(CreateLessonOutcome.Created, null, lesson);

    public static CreateLessonResult Reused(LessonResponse lesson) => new(CreateLessonOutcome.Reused, null, lesson);

    public static CreateLessonResult InvalidTopic(string error) => new(CreateLessonOutcome.InvalidTopic, error, null);

    public static CreateLessonResult LimitExceeded(string error) => new(CreateLessonOutcome.LimitExceeded, error, null);
}
