using EssayChecker.Application.DTOs.Essays;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>Esse axını: OCR, qiymətləndirmə + tarixçəyə yazma, tarixçə oxuma/silmə.</summary>
public interface IEssayService
{
    Task<EvaluateEssayResult> EvaluateAsync(int userId, EvaluateEssayRequest request, CancellationToken cancellationToken = default);

    /// <summary>9-cu sinif — DİM formatı: 3 promt-şəkli + esse mətni. Grade həmişə Grade9-dur.</summary>
    Task<EvaluateEssayResult> EvaluateGrade9WithImagesAsync(
        int userId,
        string text,
        string? title,
        IReadOnlyList<PromptImage> promptImages,
        CancellationToken cancellationToken = default);

    Task<OcrResponse> ReadImageAsync(Stream imageStream, string contentType, CancellationToken cancellationToken = default);

    Task<EssayHistoryResponse> GetHistoryAsync(int userId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<EssayDetailResponse?> GetByIdAsync(int userId, int essayId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int userId, int essayId, CancellationToken cancellationToken = default);

    /// <summary>İstifadəçinin bütün tarixçəsini silir (silinən sayı qaytarılır).</summary>
    Task<int> DeleteAllAsync(int userId, CancellationToken cancellationToken = default);
}
