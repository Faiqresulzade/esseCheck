using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>AI ilə esse qiymətləndirmə (OpenRouter).</summary>
public interface IEssayEvaluator
{
    /// <summary>
    /// <paramref name="promptImages"/> yalnız 9-cu sinifdə (şəkil-əsaslı yazı formatı) doldurulur —
    /// AI essenin bu şəkillərdə göstərilənlərə uyğun yazılıb-yazılmadığını (content balı) qiymətləndirir.
    /// 11-ci sinifdə həmişə boş/null olur, mövzu (topic) mətnlə işləyir.
    /// </summary>
    Task<EssayEvaluationData> EvaluateAsync(
        string essayText,
        GradeLevel grade,
        string? topic,
        IReadOnlyList<PromptImage>? promptImages = null,
        CancellationToken cancellationToken = default);
}
