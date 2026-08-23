using System.Security.Cryptography;
using System.Text;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using EssayChecker.Domain.Entities.Subscriptions;
using EssayChecker.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Subscriptions;

/// <summary>
/// Qeydiyyatda 1 aylıq Pro sınağı verir, amma hər cihaza YALNIZ BİR DƏFƏ.
///
/// Ardıcıllıq vacibdir: əvvəlcə cihaz "işlədilmiş" kimi qeyd olunur, YALNIZ sonra abunəlik
/// yazılır. Əks halda paralel iki qeydiyyat hər ikisi trial ala bilərdi.
/// </summary>
public sealed class TrialService : ITrialService
{
    private readonly IDeviceTrialRepository _deviceTrials;
    private readonly TrialSettings _settings;
    private readonly ILogger<TrialService> _logger;

    public TrialService(
        IDeviceTrialRepository deviceTrials,
        IOptions<TrialSettings> settings,
        ILogger<TrialService> logger)
    {
        _deviceTrials = deviceTrials;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> TryGrantAsync(
        int userId, string? deviceId, string? integrityToken, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
            return false;

        // Cihaz ID-si yoxdursa trial verilmir. Bu qəsdəndir: əks halda sahəni sadəcə
        // göndərməməklə hər dəfə yeni pulsuz ay almaq olardı.
        if (string.IsNullOrWhiteSpace(deviceId))
            return false;

        // Play Integrity qurulduqdan sonra RequireIntegrityToken=true edilir və uydurulmuş
        // cihaz ID-ləri bu yoxlamada kəsilir. Hazırda token yalnız qəbul olunur, yoxlanmır.
        if (_settings.RequireIntegrityToken && string.IsNullOrWhiteSpace(integrityToken))
        {
            _logger.LogInformation("Trial verilmədi: integrity token tələb olunur, amma göndərilməyib (userId {UserId}).", userId);
            return false;
        }

        var now = DateTime.UtcNow;

        var trial = new DeviceTrial
        {
            DeviceIdHash = Hash(deviceId),
            GrantedToUserId = userId,
            GrantedAt = now
        };

        var subscription = new UserSubscription
        {
            UserId = userId,
            Plan = _settings.Plan,
            StartDate = now,
            EndDate = now.AddDays(_settings.DurationDays),
            IsActive = true,
            AutoRenew = false,
            Platform = SubscriptionPlatform.Trial,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Cihaz qeydi və abunəlik bir tranzaksiyadadır — bax IDeviceTrialRepository.
        var granted = await _deviceTrials.TryClaimAndGrantAsync(trial, subscription, cancellationToken);

        if (!granted)
            _logger.LogInformation("Trial verilmədi: bu cihaz sınaq haqqını artıq istifadə edib (userId {UserId}).", userId);

        return granted;
    }

    /// <summary>Xam cihaz ID-si saxlanılmır — şəxsi məlumatdır, yoxlama üçün heş kifayətdir.</summary>
    private static string Hash(string deviceId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(deviceId.Trim())));
}
