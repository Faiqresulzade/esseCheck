namespace EssayChecker.Api.Admin;

/// <summary>
/// Sahibkar panelinin (Razor Pages, /admin) autentifikasiyası. Mobil API-nin JWT sxemindən
/// TAM AYRIDIR: panel brauzerdən açılır, ona görə cookie işlədir və heç bir mobil istifadəçi
/// tokeni panelə giriş vermir.
/// </summary>
public static class AdminAuth
{
    /// <summary>Cookie autentifikasiya sxeminin adı — JWT defolt sxemi ilə qarışmasın deyə açıq adlandırılıb.</summary>
    public const string Scheme = "AdminCookie";

    /// <summary>Yalnız bu sxemlə (yəni paneldən login olmuş sessiya ilə) giriş verən siyasət.</summary>
    public const string Policy = "AdminPanel";

    public const string CookieName = "essaycheck_admin";
}
