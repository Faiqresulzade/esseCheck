using System.ComponentModel.DataAnnotations;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Teaching;

/// <summary>
/// Şagird kartı. Droplist üçün <see cref="Id"/> + <see cref="FullName"/> kifayətdir;
/// <see cref="Grade"/> təyin olunubsa frontend esse formasında sinfi öncədən doldura bilər.
/// </summary>
public sealed record StudentResponse(
    int Id,
    int GroupId,
    string GroupName,
    string FullName,
    GradeLevel? Grade,
    DateTime CreatedAt);

public sealed class SaveStudentRequest
{
    [Required(ErrorMessage = "Şagirdin adı boş ola bilməz.")]
    [MaxLength(100, ErrorMessage = "Ad maksimum 100 simvol ola bilər.")]
    public string FullName { get; set; } = null!;

    /// <summary>Opsional — təyin olunmasa esse göndərilərkən sinif sorğuda göstərilməlidir.</summary>
    [EnumDataType(typeof(GradeLevel), ErrorMessage = "Sinif dəyəri etibarsızdır.")]
    public GradeLevel? Grade { get; set; }
}
