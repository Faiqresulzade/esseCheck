using EssayChecker.Domain.Entities.Subscriptions;

namespace EssayChecker.Application.DTOs.Interfaces;

public interface IDeviceTrialRepository
{
    /// <summary>
    /// Cihazı "trial istifadə edilmiş" kimi qeyd edir VƏ eyni tranzaksiyada sınaq abunəliyini
    /// yazır. Cihaz artıq qeydiyyatdadırsa (və ya paralel sorğu bizi qabaqlayıbsa) false
    /// qaytarır — bu halda heç nə yazılmır.
    ///
    /// İkisi bir tranzaksiyadadır ki, abunəlik yazıla bilmədikdə cihaz "yanmış" qalmasın:
    /// əks halda istifadəçi nə sınaq alardı, nə də bir daha ala bilərdi.
    /// </summary>
    Task<bool> TryClaimAndGrantAsync(
        DeviceTrial trial, UserSubscription subscription, CancellationToken cancellationToken = default);
}
