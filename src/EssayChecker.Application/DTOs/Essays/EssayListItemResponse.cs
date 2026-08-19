using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Essays;

public sealed record EssayListItemResponse(
    int Id,
    string Title,
    DateTime CreatedAt,
    int WordCount,
    double TotalScore,
    GradeLevel Grade,
    /// <summary>Esse bir şagird üçün yoxlanılıbsa onun id-si, əks halda null.</summary>
    int? StudentId,
    /// <summary>Şagirdin adı — siyahıda göstərmək üçün. Şagird silinsə də ad qalır.</summary>
    string? StudentName);
