using EssayChecker.Application.DTOs.Teaching;
using EssayChecker.Domain.Entities.Teaching;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>
/// Qrup/şagird oxuma-yazma. Bütün metodlar <c>teacherId</c> qəbul edir və sahibliyi ÖZLƏRİ
/// yoxlayır — başqa müəllimin qrupuna/şagirdinə müraciət "tapılmadı" kimi qayıdır, beləliklə
/// mövcudluq faktı da sızmır.
/// </summary>
public interface ITeachingRepository
{
    Task<IReadOnlyList<GroupResponse>> GetGroupsAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<int> CountGroupsAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<GroupResponse> AddGroupAsync(StudentGroup group, CancellationToken cancellationToken = default);

    Task<bool> RenameGroupAsync(int teacherId, int groupId, string name, CancellationToken cancellationToken = default);

    /// <summary>Qrupu və (kaskadla) şagirdlərini soft-delete edir. Esselər toxunulmur.</summary>
    Task<bool> DeleteGroupAsync(int teacherId, int groupId, CancellationToken cancellationToken = default);

    Task<bool> GroupExistsAsync(int teacherId, int groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentResponse>> GetStudentsAsync(int teacherId, int? groupId, CancellationToken cancellationToken = default);

    Task<StudentResponse?> GetStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default);

    Task<int> CountStudentsAsync(int teacherId, int groupId, CancellationToken cancellationToken = default);

    Task<StudentResponse> AddStudentAsync(Student student, CancellationToken cancellationToken = default);

    Task<bool> UpdateStudentAsync(int teacherId, int studentId, SaveStudentRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Esse göndərilərkən şagirdin bu müəllimə aid və silinməmiş olduğunu yoxlayır. Tapılmasa
    /// null — çağıran tərəf 400 qaytarır (başqasının şagirdinə esse yazıla bilməz).
    /// </summary>
    Task<Student?> GetOwnedStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Şagirdin adı — silinmiş şagirdlər üçün də qaytarılır, çünki keçmiş esse tarixçəsində ad
    /// görünməyə davam etməlidir. Sahiblik yoxlanmır: yalnız artıq sahibliyi təsdiqlənmiş
    /// essenin StudentId-si ilə çağırılır.
    /// </summary>
    Task<string?> GetStudentNameAsync(int studentId, CancellationToken cancellationToken = default);
}
