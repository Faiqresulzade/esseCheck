using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>Mövzu izahı (dərs): yaratma + keş, siyahı, detal, silmə.</summary>
public interface ILessonService
{
    /// <param name="grade">Controller tərəfindən həll edilmiş sinif (sorğu → şagird kartı).</param>
    /// <returns>
    /// Mövzu İngilis dilinə aid deyilsə <see cref="CreateLessonResult.Success"/> false olur —
    /// bu halda heç nə saxlanılmır və çağıran tərəf sayğacı ARTIRMAMALIDIR.
    /// </returns>
    Task<CreateLessonResult> CreateAsync(
        int userId, CreateLessonRequest request, GradeLevel grade, CancellationToken cancellationToken = default);

    Task<LessonHistoryResponse> GetHistoryAsync(
        int userId, string? search, int? studentId, int? groupId, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<LessonResponse?> GetByIdAsync(int userId, int lessonId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int userId, int lessonId, CancellationToken cancellationToken = default);
}

/// <summary>AI-dan dərs məzmunu alan servis (OpenRouter). Keş və saxlama bunun işi deyil.</summary>
public interface ILessonGenerator
{
    Task<LessonGenerationResult> GenerateAsync(
        string topic, GradeLevel grade, CancellationToken cancellationToken = default);
}

/// <summary>
/// AI nəticəsi. <see cref="IsEnglishTopic"/> false olduqda slayd/test boş olur — mövzu İngilis
/// dili dərsinə aid deyil (esse axınındakı isEssay şablonunun eynisi).
/// </summary>
public sealed record LessonGenerationResult(
    bool IsEnglishTopic,
    IReadOnlyList<LessonSlideDto> Slides,
    IReadOnlyList<LessonQuizQuestionDto> Quiz);
