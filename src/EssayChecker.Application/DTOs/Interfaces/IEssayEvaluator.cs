using EssayChecker.Application.DTOs.Essays;
using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>AI ilə esse qiymətləndirmə (OpenRouter).</summary>
public interface IEssayEvaluator
{
    Task<EssayEvaluationData> EvaluateAsync(string essayText, GradeLevel grade, string? topic, CancellationToken cancellationToken = default);
}
