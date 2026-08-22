using EssayChecker.Application.DTOs.Subscriptions;

namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>Plana və UTC günə görə istifadə limitlərini yoxlayır və sayğacları artırır.</summary>
public interface IUsageLimitService
{
    /// <summary>Text esse yoxlamasına icazə var? (sayğacı ARTIRMIR)</summary>
    Task<UsageDecision> CheckTextAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Uğurlu text yoxlamadan sonra günlük sayğacı artırır.</summary>
    Task ConsumeTextAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>OCR-a icazə var? (plan yoxlaması)</summary>
    Task<UsageDecision> CheckOcrAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Uğurlu OCR-dan sonra sayğacı artırır.</summary>
    Task ConsumeOcrAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Yeni dərs yaratmağa icazə var? Esse limitindən ayrı sayğac (sayğacı ARTIRMIR).</summary>
    Task<UsageDecision> CheckLessonAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Uğurlu dərs yaradılışından sonra gündəlik dərs sayğacını artırır.</summary>
    Task ConsumeLessonAsync(int userId, CancellationToken cancellationToken = default);

    Task<DailyUsageStatusResponse> GetStatusAsync(int userId, CancellationToken cancellationToken = default);
}
