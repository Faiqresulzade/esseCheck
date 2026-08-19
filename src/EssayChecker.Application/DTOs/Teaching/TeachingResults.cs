namespace EssayChecker.Application.DTOs.Teaching;

/// <summary>
/// Yaratma/yeniləmə nəticəsi. <see cref="EssayChecker.Application.DTOs.Essays.EvaluateEssayResult"/>
/// ilə eyni konvensiya: uğursuzluqda istifadəçiyə göstəriləcək hazır mesaj qaytarılır.
/// </summary>
public sealed record GroupResult(bool Success, string? Error, GroupResponse? Group);

public sealed record StudentResult(bool Success, string? Error, StudentResponse? Student);
