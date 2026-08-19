using EssayChecker.Application.DTOs.Teaching;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>
/// Qrup və şagird idarəetməsi. Qəsdən plandan asılı DEYİL — istənilən istifadəçi qrup/şagird
/// yarada bilər; ödənişli plan yalnız gündəlik esse limitini açır (bax IUsageLimitService).
/// </summary>
public interface ITeachingService
{
    Task<IReadOnlyList<GroupResponse>> GetGroupsAsync(int teacherId, CancellationToken cancellationToken = default);

    Task<GroupResult> CreateGroupAsync(int teacherId, SaveGroupRequest request, CancellationToken cancellationToken = default);

    Task<bool> RenameGroupAsync(int teacherId, int groupId, SaveGroupRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteGroupAsync(int teacherId, int groupId, CancellationToken cancellationToken = default);

    /// <summary>Droplist üçün: <paramref name="groupId"/> null olduqda müəllimin BÜTÜN şagirdləri.</summary>
    Task<IReadOnlyList<StudentResponse>> GetStudentsAsync(int teacherId, int? groupId, CancellationToken cancellationToken = default);

    Task<StudentResponse?> GetStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default);

    Task<StudentResult> CreateStudentAsync(int teacherId, int groupId, SaveStudentRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateStudentAsync(int teacherId, int studentId, SaveStudentRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default);
}
