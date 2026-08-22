using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>
/// Ortaq dərs kitabxanası: yaratma (limitli) və oxuma (limitsiz). Silmə qəsdən yoxdur — dərs
/// ortaq resursdur, bir istifadəçinin silməsi qalan hamını ondan məhrum edər.
/// </summary>
public interface ILessonService
{
    Task<CreateLessonResult> CreateAsync(
        int userId, CreateLessonRequest request, GradeLevel grade, CancellationToken cancellationToken = default);

    Task<LessonHistoryResponse> GetLibraryAsync(
        int userId, string? search, GradeLevel? grade, bool onlyMine, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<LessonResponse?> GetByIdAsync(int userId, int lessonId, CancellationToken cancellationToken = default);
}

/// <summary>AI-dan dərs məzmunu alan servis (OpenRouter). Kitabxana və saxlama bunun işi deyil.</summary>
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
