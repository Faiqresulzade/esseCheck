namespace EssayChecker.Application.DTOs.Interfaces;

/// <summary>Qeydiyyatda cihaza bağlı pulsuz sınaq abunəliyi verir.</summary>
public interface ITrialService
{
    /// <summary>
    /// Şərtlər ödənirsə istifadəçiyə sınaq abunəliyi yazır. Trial verilmədikdə (cihaz ID-si
    /// yoxdur, cihaz artıq istifadə edib, funksiya söndürülüb) sadəcə false qaytarır —
    /// qeydiyyat HEÇ VAXT bu səbəbdən uğursuz olmamalıdır.
    /// </summary>
    Task<bool> TryGrantAsync(
        int userId, string? deviceId, string? integrityToken, CancellationToken cancellationToken = default);
}
