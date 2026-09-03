using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EssayChecker.Api.Admin;
using EssayChecker.Application.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace EssayChecker.Api.Pages.Admin;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly AdminSettings _settings;

    public LoginModel(IOptions<AdminSettings> settings)
    {
        _settings = settings.Value;
    }

    public string? Error { get; private set; }

    public IActionResult OnGet()
    {
        // Açar konfiqurasiya olunmayıbsa panel ümumiyyətlə mövcud deyil (bax AdminSettings).
        if (!_settings.IsConfigured)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? key)
    {
        if (!_settings.IsConfigured)
            return NotFound();

        if (!IsValidKey(key))
        {
            // Açarın səhv olduğunu deyirik, amma nə uzunluq, nə də başqa ipucu vermirik.
            Error = "Açar yanlışdır.";
            return Page();
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "owner") },
            AdminAuth.Scheme);

        await HttpContext.SignInAsync(
            AdminAuth.Scheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return Redirect("/admin");
    }

    /// <summary>Sabit vaxtlı müqayisə — açarı simvol-simvol tapmağa imkan verməmək üçün.</summary>
    private bool IsValidKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        var provided = Encoding.UTF8.GetBytes(key);
        var expected = Encoding.UTF8.GetBytes(_settings.ApiKey);

        return provided.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(provided, expected);
    }
}

[AllowAnonymous]
public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(AdminAuth.Scheme);
        return Redirect("/admin/login");
    }

    public IActionResult OnGet() => Redirect("/admin/login");
}
