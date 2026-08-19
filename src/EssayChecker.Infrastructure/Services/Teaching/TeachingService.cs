using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Teaching;
using EssayChecker.Domain.Entities.Teaching;

namespace EssayChecker.Infrastructure.Services.Teaching;

/// <summary>
/// Qrup/şagird idarəetməsi. Plan yoxlaması QƏSDƏN yoxdur — qrup qurmaq hamıya açıqdır,
/// ödənişli plan yalnız gündəlik esse limitini artırır. Aşağıdakı hədlər isə sui-istifadəyə
/// (minlərlə qrup/şagird yaradıb bazanı şişirtmək) qarşı sadə qoruyucudur.
/// </summary>
public sealed class TeachingService : ITeachingService
{
    private const int MaxGroupsPerTeacher = 50;
    private const int MaxStudentsPerGroup = 200;

    private readonly ITeachingRepository _repository;

    public TeachingService(ITeachingRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<GroupResponse>> GetGroupsAsync(int teacherId, CancellationToken cancellationToken = default) =>
        _repository.GetGroupsAsync(teacherId, cancellationToken);

    public async Task<GroupResult> CreateGroupAsync(
        int teacherId, SaveGroupRequest request, CancellationToken cancellationToken = default)
    {
        var count = await _repository.CountGroupsAsync(teacherId, cancellationToken);
        if (count >= MaxGroupsPerTeacher)
            return new GroupResult(false, $"Maksimum {MaxGroupsPerTeacher} qrup yarada bilərsiniz.", null);

        var group = new StudentGroup
        {
            TeacherId = teacherId,
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddGroupAsync(group, cancellationToken);
        return new GroupResult(true, null, created);
    }

    public Task<bool> RenameGroupAsync(
        int teacherId, int groupId, SaveGroupRequest request, CancellationToken cancellationToken = default) =>
        _repository.RenameGroupAsync(teacherId, groupId, request.Name.Trim(), cancellationToken);

    public Task<bool> DeleteGroupAsync(int teacherId, int groupId, CancellationToken cancellationToken = default) =>
        _repository.DeleteGroupAsync(teacherId, groupId, cancellationToken);

    public Task<IReadOnlyList<StudentResponse>> GetStudentsAsync(
        int teacherId, int? groupId, CancellationToken cancellationToken = default) =>
        _repository.GetStudentsAsync(teacherId, groupId, cancellationToken);

    public Task<StudentResponse?> GetStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default) =>
        _repository.GetStudentAsync(teacherId, studentId, cancellationToken);

    public async Task<StudentResult> CreateStudentAsync(
        int teacherId, int groupId, SaveStudentRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _repository.GroupExistsAsync(teacherId, groupId, cancellationToken))
            return new StudentResult(false, "Qrup tapılmadı.", null);

        var count = await _repository.CountStudentsAsync(teacherId, groupId, cancellationToken);
        if (count >= MaxStudentsPerGroup)
            return new StudentResult(false, $"Bir qrupda maksimum {MaxStudentsPerGroup} şagird ola bilər.", null);

        var student = new Student
        {
            GroupId = groupId,
            FullName = request.FullName.Trim(),
            Grade = request.Grade,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddStudentAsync(student, cancellationToken);
        return new StudentResult(true, null, created);
    }

    public Task<bool> UpdateStudentAsync(
        int teacherId, int studentId, SaveStudentRequest request, CancellationToken cancellationToken = default)
    {
        request.FullName = request.FullName.Trim();
        return _repository.UpdateStudentAsync(teacherId, studentId, request, cancellationToken);
    }

    public Task<bool> DeleteStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default) =>
        _repository.DeleteStudentAsync(teacherId, studentId, cancellationToken);
}
