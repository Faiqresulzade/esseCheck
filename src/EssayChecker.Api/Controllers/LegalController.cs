using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

/// <summary>
/// Google Play Store listinq üçün tələb olunan ictimai hüquqi səhifələr
/// (Privacy Policy / Terms of Service). Autentifikasiya tələb etmir.
/// </summary>
[AllowAnonymous]
[Route("legal")]
[ApiController]
public class LegalController : ControllerBase
{
    private const string EffectiveDate = "23 iyul 2026";
    private const string ContactEmail = "faigrasulzada@gmail.com";

    [HttpGet("privacy-policy")]
    public ContentResult PrivacyPolicy() =>
        Content(BuildPage("Gizlilik Siyasəti", PrivacyPolicyBody()), "text/html; charset=utf-8");

    [HttpGet("terms-of-service")]
    public ContentResult TermsOfService() =>
        Content(BuildPage("İstifadə Şərtləri", TermsOfServiceBody()), "text/html; charset=utf-8");

    private static string BuildPage(string title, string bodyHtml) => $$"""
        <!DOCTYPE html>
        <html lang="az">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{{title}} — EssayCheck AI</title>
          <style>
            body { font-family: Arial, Helvetica, sans-serif; max-width: 720px; margin: 0 auto; padding: 32px 20px 80px; color: #1f2937; line-height: 1.6; }
            h1 { color: #2563eb; font-size: 26px; margin-bottom: 4px; }
            h2 { color: #111827; font-size: 18px; margin-top: 32px; }
            p, li { font-size: 15px; }
            .meta { color: #6b7280; font-size: 13px; margin-bottom: 24px; }
            a { color: #2563eb; }
          </style>
        </head>
        <body>
          <h1>{{title}}</h1>
          <p class="meta">EssayCheck AI &middot; Qüvvəyə minmə tarixi: {{EffectiveDate}}</p>
          {{bodyHtml}}
        </body>
        </html>
        """;

    private static string PrivacyPolicyBody() => """
        <p>EssayCheck AI ("biz", "tətbiq") istifadəçilərinin məxfiliyinə hörmət edir. Bu sənəd hansı məlumatları
        topladığımızı, necə istifadə etdiyimizi və hüquqlarınızı izah edir.</p>

        <h2>1. Topladığımız məlumatlar</h2>
        <ul>
          <li><strong>Hesab məlumatları:</strong> ad və soyad, e-mail ünvanı, şifrənin hash-lənmiş forması (şifrənin özü heç vaxt saxlanılmır).</li>
          <li><strong>Esse məzmunu:</strong> qiymətləndirmə üçün göndərdiyiniz mətn və ya şəkillər (OCR üçün).</li>
          <li><strong>İstifadə məlumatları:</strong> qiymətləndirmə tarixçəsi, gündəlik istifadə sayğacları, cari abunəlik planı.</li>
          <li><strong>Ödəniş məlumatları:</strong> yalnız Google Play-in verdiyi satınalma tokeni; kart məlumatlarınız bizə heç vaxt ötürülmür, tamamilə Google Play tərəfindən idarə olunur.</li>
          <li><strong>Texniki məlumatlar:</strong> giriş sessiyaları üçün access/refresh tokenlər.</li>
        </ul>

        <h2>2. Məlumatların istifadəsi</h2>
        <ul>
          <li>Essenizi süni intellekt vasitəsilə qiymətləndirmək və nəticəni sizə göstərmək.</li>
          <li>Hesabınızı idarə etmək (giriş, şifrə sıfırlama, abunəlik statusu).</li>
          <li>Gündəlik istifadə limitlərini tətbiq etmək (Free/Pro/Pro Plus planlarına görə).</li>
          <li>Texniki dəstək göstərmək.</li>
        </ul>

        <h2>3. Üçüncü tərəflərlə paylaşım</h2>
        <p>Məlumatlarınız aşağıdakı xidmətlərə, yalnız göstərilən məqsədlə ötürülür:</p>
        <ul>
          <li><strong>OpenRouter (AI xidməti):</strong> esse mətninizi qiymətləndirmək üçün.</li>
          <li><strong>Google Play Billing:</strong> abunəlik ödənişlərini emal etmək üçün.</li>
          <li><strong>E-mail xidməti (SMTP):</strong> şifrə sıfırlama və hesab bildirişləri göndərmək üçün.</li>
        </ul>
        <p>Məlumatlarınız heç bir marketinq və ya reklam məqsədilə satılmır və ya üçüncü tərəflərə paylaşılmır.</p>

        <h2>4. Məlumatların saxlanması və silinməsi</h2>
        <p>Hesabınızı istədiyiniz zaman tətbiqin <strong>Ayarlar</strong> bölməsindən silə bilərsiniz. Hesab silindikdə
        girişiniz və aktiv sessiyalarınız dərhal bağlanır. Esse tarixçənizi də ayrıca, istənilən vaxt silə bilərsiniz.</p>

        <h2>5. Uşaqların məxfiliyi</h2>
        <p>Tətbiq 13 yaşından kiçik uşaqlar üçün nəzərdə tutulmayıb və onlardan bilərəkdən məlumat toplamırıq.</p>

        <h2>6. Təhlükəsizlik</h2>
        <p>Şifrələr hash-lənmiş formada saxlanılır, bütün giriş sessiyaları JWT token-lərlə qorunur, məlumat ötürülməsi
        HTTPS üzərindən şifrələnir.</p>

        <h2>7. Dəyişikliklər</h2>
        <p>Bu siyasət zaman-zaman yenilənə bilər. Əhəmiyyətli dəyişikliklərdə tətbiq daxilində bildiriş veriləcək.</p>

        <h2>8. Əlaqə</h2>
        <p>Suallarınız üçün: <a href="mailto:__CONTACT_EMAIL__">__CONTACT_EMAIL__</a></p>
        """.Replace("__CONTACT_EMAIL__", ContactEmail);

    private static string TermsOfServiceBody() => """
        <p>EssayCheck AI tətbiqindən istifadə etməklə aşağıdakı şərtləri qəbul etmiş olursunuz.</p>

        <h2>1. Xidmətin təsviri</h2>
        <p>EssayCheck AI, ingilis dilində yazılmış esseləri Dövlət İmtahan Mərkəzi (DİM) meyarlarına əsaslanaraq
        süni intellekt vasitəsilə qiymətləndirən köməkçi bir vasitədir. Nəticələr <strong>tövsiyə xarakterlidir</strong>
        və rəsmi DİM qiymətləndirməsini əvəz etmir.</p>

        <h2>2. Hesab</h2>
        <p>Qeydiyyat zamanı düzgün məlumat verməyi öhtəsinə götürürsünüz. Hesabınızın təhlükəsizliyinə görə (şifrənizi
        kimsə ilə paylaşmamaq) siz məsuliyyət daşıyırsınız.</p>

        <h2>3. Planlar və ödəniş</h2>
        <ul>
          <li><strong>Free:</strong> gündə 1 pulsuz mətn yoxlaması.</li>
          <li><strong>Pro / Pro Plus:</strong> Google Play vasitəsilə aylıq abunəlik, avtomatik yenilənir.</li>
          <li>Abunəliyi istənilən vaxt Google Play &rarr; Subscriptions bölməsindən ləğv edə bilərsiniz.</li>
          <li>Ödənişlər Google Play Billing tərəfindən idarə olunur, geri qaytarma siyasəti Google Play qaydalarına tabedir.</li>
        </ul>

        <h2>4. Qadağan olunan istifadə</h2>
        <p>Xidməti qanunsuz məqsədlərlə, sistemi həddindən artıq yükləməklə və ya digər istifadəçilərə zərər verəcək
        şəkildə istifadə etmək qadağandır. Bu qaydaların pozulması hesabın bloklanmasına səbəb ola bilər.</p>

        <h2>5. Məzmun mülkiyyəti</h2>
        <p>Göndərdiyiniz esse mətnlərinin müəllif hüququ sizə məxsus qalır. Bu məzmun yalnız qiymətləndirmə
        məqsədilə emal olunur.</p>

        <h2>6. Məsuliyyətin məhdudlaşdırılması</h2>
        <p>Xidmət "olduğu kimi" təqdim olunur. Süni intellektin qiymətləndirməsində xətalar ola bilər, buna görə
        tətbiqin nəticələrinə əsaslanaraq alınan qərarlara görə məsuliyyət daşımırıq.</p>

        <h2>7. Xidmətin dəyişdirilməsi</h2>
        <p>Xidməti istənilən vaxt yeniləmək, dəyişdirmək və ya dayandırmaq hüququnu özümüzdə saxlayırıq.</p>

        <h2>8. Əlaqə</h2>
        <p>Suallarınız üçün: <a href="mailto:__CONTACT_EMAIL__">__CONTACT_EMAIL__</a></p>
        """.Replace("__CONTACT_EMAIL__", ContactEmail);
}
