using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Domain.Entities.Lessons;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface ILessonRepository
{
    Task AddAsync(Lesson lesson, CancellationToken cancellationToken = default);

    Task<Lesson?> GetByIdAsync(int userId, int lessonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// İstifadəçinin eyni mövzu+sinif üzrə mövcud dərsi. Varsa yenidən yaradılmır və limit
    /// xərclənmir — istifadəçi öz siyahısındakı dərsi açır.
    /// </summary>
    Task<Lesson?> FindOwnAsync(
        int userId, string normalizedTopic, GradeLevel grade, CancellationToken cancellationToken = default);

    Task<LessonHistoryResponse> GetHistoryAsync(
        int userId, string? search, int? studentId, int? groupId, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int userId, int lessonId, CancellationToken cancellationToken = default);

    /// <summary>Keş axtarışı: mövzu+sinif+prompt versiyası. Tapılmasa AI çağırılır.</summary>
    Task<LessonTemplate?> FindTemplateAsync(
        string normalizedTopic, GradeLevel grade, int promptVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keşə yeni şablon yazır. Eyni açar paralel sorğuda artıq yaradılıbsa səssizcə keçir —
    /// unikal indeks pozuntusu istifadəçiyə xəta kimi çıxmamalıdır.
    /// </summary>
    Task AddTemplateAsync(LessonTemplate template, CancellationToken cancellationToken = default);
}
