using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

/// <summary>
/// E-mail ilə göndərilən şifrə sıfırlama linkinin açdığı ictimai veb səhifə.
/// Tətbiqin ayrıca veb frontend-i olmadığı üçün səhifə birbaşa backend tərəfindən verilir —
/// beləliklə link istənilən cihazda, tətbiq quraşdırılmasa belə işləyir.
/// Səhifə yalnız formadır; şifrəni faktiki dəyişən məntiq /api/Auth/reset-password endpoint-indədir.
/// </summary>
[AllowAnonymous]
[Route("reset-password")]
public class ResetPasswordPageController : ControllerBase
{
    [HttpGet]
    public ContentResult Index([FromQuery] string? email, [FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return Html(BuildPage(InvalidLinkBody()));

        // email və token sorğudan gəlir — HTML-ə yerləşdirilməzdən əvvəl mütləq encode edilir ki,
        // linkə əl gəzdirməklə səhifəyə skript yeridilə bilməsin (XSS).
        return Html(BuildPage(FormBody(WebUtility.HtmlEncode(email), WebUtility.HtmlEncode(token))));
    }

    private ContentResult Html(string html) => Content(html, "text/html; charset=utf-8");

    private static string InvalidLinkBody() => """
        <h1>Link etibarsızdır</h1>
        <p class="lead">Bu şifrə sıfırlama linki natamam və ya səhvdir.</p>
        <p>Zəhmət olmasa tətbiqdə <strong>"Şifrəni unutmusunuz?"</strong> bölməsindən yeni link tələb edin.
        Linklərin müəyyən müddətdən sonra etibarını itirdiyini nəzərə alın.</p>
        """;

    private static string FormBody(string encodedEmail, string encodedToken) => $$"""
        <h1>Yeni şifrə təyin edin</h1>
        <p class="lead">Hesab: <strong>{{encodedEmail}}</strong></p>

        <form id="form" autocomplete="off">
          <input type="hidden" id="email" value="{{encodedEmail}}" />
          <input type="hidden" id="token" value="{{encodedToken}}" />

          <label for="password">Yeni şifrə</label>
          <input type="password" id="password" required minlength="8" placeholder="Ən azı 8 simvol" />

          <label for="confirm">Yeni şifrə (təkrar)</label>
          <input type="password" id="confirm" required minlength="8" placeholder="Şifrəni təkrar yazın" />

          <button type="submit" id="submit">Şifrəni dəyiş</button>
        </form>

        <div id="result" class="result" hidden></div>

        <script>
          const form = document.getElementById('form');
          const submitBtn = document.getElementById('submit');
          const result = document.getElementById('result');

          function show(message, ok) {
            result.textContent = message;
            result.className = 'result ' + (ok ? 'ok' : 'err');
            result.hidden = false;
          }

          form.addEventListener('submit', async (e) => {
            e.preventDefault();

            const password = document.getElementById('password').value;
            const confirm = document.getElementById('confirm').value;

            if (password !== confirm) {
              show('Şifrələr uyğun gəlmir.', false);
              return;
            }

            submitBtn.disabled = true;
            submitBtn.textContent = 'Göndərilir...';

            try {
              const response = await fetch('/api/Auth/reset-password', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                  email: document.getElementById('email').value,
                  token: document.getElementById('token').value,
                  newPassword: password,
                  confirmPassword: confirm
                })
              });

              const data = await response.json().catch(() => null);

              if (response.ok) {
                form.hidden = true;
                show('Şifrəniz uğurla dəyişdirildi. İndi tətbiqə yeni şifrənizlə daxil ola bilərsiniz.', true);
                return;
              }

              show((data && data.message) || 'Şifrə dəyişdirilmədi. Linkin vaxtı bitmiş ola bilər.', false);
            } catch {
              show('Şəbəkə xətası. İnternet bağlantınızı yoxlayıb yenidən cəhd edin.', false);
            } finally {
              submitBtn.disabled = false;
              submitBtn.textContent = 'Şifrəni dəyiş';
            }
          });
        </script>
        """;

    private static string BuildPage(string bodyHtml) => $$"""
        <!DOCTYPE html>
        <html lang="az">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Şifrə sıfırlama — EssayCheck AI</title>
          <style>
            body { font-family: Arial, Helvetica, sans-serif; max-width: 420px; margin: 0 auto; padding: 40px 20px 80px; color: #1f2937; line-height: 1.6; }
            .brand { color: #2563eb; font-size: 22px; font-weight: bold; margin-bottom: 28px; }
            h1 { font-size: 22px; margin: 0 0 8px; }
            .lead { color: #6b7280; font-size: 14px; margin-top: 0; }
            label { display: block; font-size: 14px; font-weight: bold; margin: 20px 0 6px; }
            input[type=password] { width: 100%; padding: 12px; font-size: 15px; border: 1px solid #d1d5db; border-radius: 8px; box-sizing: border-box; }
            input[type=password]:focus { outline: none; border-color: #2563eb; }
            button { width: 100%; margin-top: 28px; background: #2563eb; color: #fff; border: 0; padding: 14px; font-size: 15px; font-weight: bold; border-radius: 8px; cursor: pointer; }
            button:disabled { background: #9ca3af; cursor: default; }
            .result { margin-top: 24px; padding: 14px; border-radius: 8px; font-size: 14px; }
            .result.ok { background: #ecfdf5; color: #065f46; }
            .result.err { background: #fef2f2; color: #991b1b; }
          </style>
        </head>
        <body>
          <div class="brand">EssayCheck AI</div>
          {{bodyHtml}}
        </body>
        </html>
        """;
}
