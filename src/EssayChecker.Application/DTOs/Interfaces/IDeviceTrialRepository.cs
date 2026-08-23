using EssayChecker.Domain.Entities.Subscriptions;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface IDeviceTrialRepository
{
    /// <summary>
    /// Cihazı "trial istifadə edilmiş" kimi qeyd edir. Cihaz artıq qeydiyyatdadırsa (və ya
    /// paralel sorğu bizi qabaqlayıbsa) false qaytarır — bu halda trial VERİLMƏMƏLİDİR.
    /// </summary>
    Task<bool> TryClaimAsync(DeviceTrial trial, CancellationToken cancellationToken = default);
}
