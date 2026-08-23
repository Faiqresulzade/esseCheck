using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Domain.Entities.Lessons;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>
/// Dərs kitabxanası. Sahiblik yoxlaması YOXDUR — dərslər ortaqdır, hər istifadəçi hamısını
/// oxuya bilər. <c>currentUserId</c> yalnız cavabdakı <c>isMine</c> bayrağını doldurmaq üçündür.
/// </summary>
public interface ILessonRepository
{
    Task AddAsync(Lesson lesson, CancellationToken cancellationToken = default);

    Task<Lesson?> GetByIdAsync(int lessonId, CancellationToken cancellationToken = default);

    /// <summary>Kitabxanada bu mövzu+sinif üçün dərs varmı — varsa AI çağırılmır və limit toxunulmur.</summary>
    Task<Lesson?> FindByTopicAsync(
        string normalizedTopic, GradeLevel grade, CancellationToken cancellationToken = default);

    /// <param name="onlyMine">true olduqda yalnız bu istifadəçinin yaratdığı dərslər.</param>
    Task<LessonHistoryResponse> GetLibraryAsync(
        int currentUserId, string? search, GradeLevel? grade, bool onlyMine, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dərsi yaradanın adı — cavabda göstərmək üçün. Hesab silinibsə (CreatedByUserId null) və ya
    /// tapılmasa null qaytarır; çağıran tərəf əvəzedici mətn qoyur.
    /// </summary>
    Task<string?> GetCreatorNameAsync(int? userId, CancellationToken cancellationToken = default);
}
