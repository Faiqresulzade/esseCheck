namespace EssayChecker.Application.Settings;

/// <summary>
/// Yalnız closed testing mərhələsi üçün müvəqqəti bayraqlar. Production-a çıxarkən
/// <see cref="ForceProPlusForAllUsers"/> mütləq false olmalıdır (appsettings.json-dakı
/// defolt dəyər elə buna görə false-dur — yalnız Render env var ilə açılır).
/// </summary>
public sealed class TestingSettings
{
    public const string SectionName = "Testing";

    /// <summary>
    /// true olduqda bütün istifadəçilər real abunəliklərindən asılı olmayaraq Pro Plus
    /// hüquqları (limitsiz mətn + OCR) alır. Google Play-in test alışlarını sürətləndirilmiş
    /// dövrədə avtomatik ləğv etməsi closed testing zamanı testçiləri narahat etdiyi üçün
    /// əlavə olunub — real billing/verify axını toxunulmamış qalır, sadəcə limit tətbiqi
    /// müvəqqəti dayandırılır.
    /// </summary>
    public bool ForceProPlusForAllUsers { get; set; } = false;
}
