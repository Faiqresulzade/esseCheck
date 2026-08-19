using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Teaching;
using EssayChecker.Domain.Entities.Teaching;
using EssayChecker.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace EssayChecker.Persistence.Repositories;

public sealed class TeachingRepository : ITeachingRepository
{
    private readonly EssayDbContext _db;

    public TeachingRepository(EssayDbContext db)
    {
        _db = db;
    }

    // --- Qruplar ---

    private IQueryable<StudentGroup> OwnedGroups(int teacherId) =>
        _db.StudentGroups.Where(g => g.TeacherId == teacherId && !g.IsDeleted);

    public async Task<IReadOnlyList<GroupResponse>> GetGroupsAsync(int teacherId, CancellationToken cancellationToken = default) =>
        await OwnedGroups(teacherId)
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new GroupResponse(
                g.Id,
                g.Name,
                g.Students.Count(s => !s.IsDeleted),
                g.CreatedAt))
            .ToListAsync(cancellationToken);

    public Task<int> CountGroupsAsync(int teacherId, CancellationToken cancellationToken = default) =>
        OwnedGroups(teacherId).CountAsync(cancellationToken);

    public async Task<GroupResponse> AddGroupAsync(StudentGroup group, CancellationToken cancellationToken = default)
    {
        _db.StudentGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);
        return new GroupResponse(group.Id, group.Name, 0, group.CreatedAt);
    }

    public async Task<bool> RenameGroupAsync(int teacherId, int groupId, string name, CancellationToken cancellationToken = default)
    {
        var affected = await OwnedGroups(teacherId)
            .Where(g => g.Id == groupId)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.Name, name), cancellationToken);

        return affected > 0;
    }

    public async Task<bool> DeleteGroupAsync(int teacherId, int groupId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var affected = await OwnedGroups(teacherId)
            .Where(g => g.Id == groupId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.IsDeleted, true)
                .SetProperty(g => g.DeletedAt, now), cancellationToken);

        if (affected == 0)
            return false;

        // Qrupun şagirdləri də siyahıdan çıxır — esseləri isə toxunulmur (StudentId qalır).
        await _db.Students
            .Where(s => s.GroupId == groupId && !s.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.DeletedAt, now), cancellationToken);

        return true;
    }

    public Task<bool> GroupExistsAsync(int teacherId, int groupId, CancellationToken cancellationToken = default) =>
        OwnedGroups(teacherId).AnyAsync(g => g.Id == groupId, cancellationToken);

    // --- Şagirdlər ---

    private IQueryable<Student> OwnedStudents(int teacherId) =>
        _db.Students.Where(s =>
            !s.IsDeleted && !s.Group.IsDeleted && s.Group.TeacherId == teacherId);

    public async Task<IReadOnlyList<StudentResponse>> GetStudentsAsync(
        int teacherId, int? groupId, CancellationToken cancellationToken = default)
    {
        var query = OwnedStudents(teacherId).AsNoTracking();

        if (groupId is not null)
            query = query.Where(s => s.GroupId == groupId);

        return await query
            .OrderBy(s => s.Group.Name)
            .ThenBy(s => s.FullName)
            .Select(s => new StudentResponse(s.Id, s.GroupId, s.Group.Name, s.FullName, s.Grade, s.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<StudentResponse?> GetStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default) =>
        OwnedStudents(teacherId)
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new StudentResponse(s.Id, s.GroupId, s.Group.Name, s.FullName, s.Grade, s.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountStudentsAsync(int teacherId, int groupId, CancellationToken cancellationToken = default) =>
        OwnedStudents(teacherId).CountAsync(s => s.GroupId == groupId, cancellationToken);

    public async Task<StudentResponse> AddStudentAsync(Student student, CancellationToken cancellationToken = default)
    {
        _db.Students.Add(student);
        await _db.SaveChangesAsync(cancellationToken);

        var groupName = await _db.StudentGroups
            .Where(g => g.Id == student.GroupId)
            .Select(g => g.Name)
            .FirstAsync(cancellationToken);

        return new StudentResponse(
            student.Id, student.GroupId, groupName, student.FullName, student.Grade, student.CreatedAt);
    }

    public async Task<bool> UpdateStudentAsync(
        int teacherId, int studentId, SaveStudentRequest request, CancellationToken cancellationToken = default)
    {
        var affected = await OwnedStudents(teacherId)
            .Where(s => s.Id == studentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.FullName, request.FullName)
                .SetProperty(x => x.Grade, request.Grade), cancellationToken);

        return affected > 0;
    }

    public async Task<bool> DeleteStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var affected = await OwnedStudents(teacherId)
            .Where(s => s.Id == studentId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.DeletedAt, now), cancellationToken);

        return affected > 0;
    }

    public Task<Student?> GetOwnedStudentAsync(int teacherId, int studentId, CancellationToken cancellationToken = default) =>
        OwnedStudents(teacherId)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

    public Task<string?> GetStudentNameAsync(int studentId, CancellationToken cancellationToken = default) =>
        _db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => s.FullName)
            .FirstOrDefaultAsync(cancellationToken);
}
