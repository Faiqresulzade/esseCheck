using EssayChecker.Application.DTOs.Essays;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>AI ilə esse qiymətləndirmə (OpenRouter).</summary>
public interface IEssayEvaluator
{
    Task<EssayEvaluationData> EvaluateAsync(string essayText, CancellationToken cancellationToken = default);
}
