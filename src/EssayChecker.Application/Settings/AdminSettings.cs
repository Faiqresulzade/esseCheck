namespace EssayChecker.Application.Settings;

/// <summary>
/// Sahibkar (owner) statistika endpoint-lərinin qorunması. Bu endpoint-lər BÜTÜN istifadəçilərin
/// adını, e-mailini və abunə vəziyyətini qaytarır — ona görə adi JWT [Authorize] kifayət etmir,
/// əks halda hər qeydiyyatlı istifadəçi hamının şəxsi məlumatını görə bilərdi.
///
/// <see cref="ApiKey"/> boş qalarsa endpoint-lər ümumiyyətlə mövcud olmur (404) — yəni açarı
/// təyin etməyi unutmaq məlumatı açıq qoymur, tam əksinə, funksiyanı bağlayır.
/// Production-da Render env var ilə verilir: Admin__ApiKey
/// </summary>
public sealed class AdminSettings
{
    public const string SectionName = "Admin";

    public string ApiKey { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
