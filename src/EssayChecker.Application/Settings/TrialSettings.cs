using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.Settings;

/// <summary>
/// Qeydiyyatda avtomatik verilən pulsuz sınaq abunəliyi. Cihaza bağlıdır — bax DeviceTrial.
/// </summary>
public sealed class TrialSettings
{
    public const string SectionName = "Trial";

    /// <summary>false olduqda heç kimə trial verilmir (funksiyanı tez söndürmək üçün).</summary>
    public bool Enabled { get; set; } = true;

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Pro;

    public int DurationDays { get; set; } = 30;

    /// <summary>
    /// Play Integrity qurulduqdan SONRA true edilməlidir. true olduqda cihaz ID-si yalnız
    /// etibarlı integrity token ilə birlikdə qəbul olunur — yəni skriptlə uydurulmuş ID trial
    /// ala bilmir. Hazırda false: Play Integrity hələ qurulmayıb, ona görə ANDROID_ID tək başına
    /// qəbul edilir və bu müddətdə texniki bilikli istifadəçi qorumanı keçə bilər (məlum və
    /// qəbul edilmiş risk — bax FRONTEND_TRIAL.md).
    /// </summary>
    public bool RequireIntegrityToken { get; set; } = false;
}
