namespace EssayChecker.Domain.Entities.Subscriptions;

/// <summary>
/// Bir cihazın pulsuz sınaq (trial) haqqını istifadə etdiyini qeyd edir.
///
/// Məqsəd: istifadəçi bir ay bitəndən sonra yeni hesab açıb yenidən pulsuz Pro almasın. Qeyd
/// istifadəçiyə deyil, CİHAZA bağlıdır və hesab silinsə belə qalır — əks halda "hesabı sil,
/// yenisini aç" ilə qoruma asanlıqla keçilərdi.
/// </summary>
public class DeviceTrial
{
    public int Id { get; set; }

    /// <summary>
    /// Cihaz identifikatorunun SHA-256 heşi. Xam ID saxlanılmır — bu, şəxsi məlumatdır və
    /// yoxlama üçün heş kifayətdir (RefreshToken-lərdəki eyni prinsip).
    /// </summary>
    public string DeviceIdHash { get; set; } = null!;

    /// <summary>Bu cihazda trial-ı ilk alan istifadəçi (audit üçün; hesab silinsə də qalır).</summary>
    public int GrantedToUserId { get; set; }

    public DateTime GrantedAt { get; set; }
}
