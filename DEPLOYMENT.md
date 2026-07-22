# EssayCheck AI — Deployment Bələdçisi

Bu sənəd backend-i (EssayChecker.Api) production-a necə çıxaracağını izah edir. Bütün dəyişikliklər **real Docker konteynerində, sıfırdan PostgreSQL ilə test edilib** (aşağıda "Doğrulama" bölməsinə bax).

> **2026-07-22 yeniləmə:** Verilənlər bazası **SQL Server-dən PostgreSQL-ə keçirilib** (`Npgsql.EntityFrameworkCore.PostgreSQL`). Bütün köhnə SQL Server migration-ları silinib, PostgreSQL üçün təzə konsolidasiya olunmuş `InitialPostgresMigration` yaradılıb.

---

## 1. Nə dəyişdi (bu sənədə uyğunlaşdırma üçün)

| Fayl | Dəyişiklik |
|---|---|
| `appsettings.json` | Bütün real sirlər (`Jwt:Key`, `Email:Password`, `ConnectionStrings:DefaultConnection`, e-mail ünvanları) boşaldıldı. Yerli development üçün bu dəyərlər artıq **`dotnet user-secrets`**-dədir (commit olunmur). |
| `appsettings.Production.json` | **Yeni** — yalnız log səviyyəsini (`Warning`) tənzimləyir, heç bir sirr yoxdur. |
| `Program.cs` | 3 əlavə: (1) `ForwardedHeaders` middleware — reverse proxy (Nginx/IIS/Azure) arxasında düzgün HTTPS aşkarlanması üçün; (2) CORS `Cors:AllowedOrigins` konfiqurasiyasından oxunur (boşdursa hamısına icazə — mobil app-lər üçün problem deyil); (3) **tətbiq başlayanda avtomatik `dbContext.Database.MigrateAsync()`** — server-də əl ilə `dotnet ef database update` işlətməyi unutmaq riski aradan qalxır. |
| `Dockerfile`, `.dockerignore` | **Yeni** — multi-stage build, .NET 8 runtime image, port 8080. |
| `.gitignore` | **Yeni** — `bin/`, `obj/`, `.vs/`, sirr faylları, `Yeni klasör/` (alaqiddi tooling checkout-u). |
| `EssayChecker.Persistence` provideri | `Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.PostgreSQL`. Köhnə SQL Server migration-ları silinib, `InitialPostgresMigration` yaradılıb. |
| `docker-compose.dev.yml` | **Yeni** — yalnız yerli development üçün, named volume ilə davamlı PostgreSQL. |

---

## 2. Environment Variables (production-da MÜTLƏQ təyin olunmalı)

ASP.NET Core konfiqurasiyası `Section:Key` formatını environment variable-larda **`Section__Key`** (ikiqat alt xətt) kimi oxuyur.

| Env var | Nümunə dəyər | Qeyd |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Swagger-i söndürür, `appsettings.Production.json`-u aktivləşdirir |
| `ConnectionStrings__DefaultConnection` | `Host=...;Port=5432;Database=EssayCheckDb;Username=...;Password=...;SSL Mode=Require` | Bax bölmə 3 (PostgreSQL) |
| `Jwt__Key` | (ən azı 32 simvol, təsadüfi) | JWT imzalama açarı — sızmasın |
| `Email__SenderEmail` | `noreply@sənindomain.com` | Gmail istifadə edirsənsə, [App Password](https://myaccount.google.com/apppasswords) yarat |
| `Email__Username` | | |
| `Email__Password` | | Gmail adi şifrəsi YOX, App Password |
| `App__ResetPasswordUrl` | `https://app.essaycheck.az/reset-password` | Real frontend/deep-link ünvanı |
| `OpenRouter__ApiKey` | `sk-or-v1-...` | **Köhnə açarı ləğv et** (əvvəllər bu söhbətdə açıq göründüyü üçün) |
| `GooglePlay__PackageName` | `az.essaycheck.app` | Google Play Billing üçün (bax `FRONTEND_GOOGLE_PLAY_BILLING.md`) |
| `GooglePlay__ServiceAccountJsonPath` | `/app/secrets/google-play-service-account.json` | Fayl **volume/bind-mount ilə** verilməlidir, image-ə bakedlənmir |
| `GooglePlay__RtdnSharedSecret` | (təsadüfi, 16+ simvol) | RTDN webhook-u qorumaq üçün |

**Bunlar `appsettings.json`-da artıq qeyri-sirr dəyərlərlə mövcuddur, adətən dəyişməyə ehtiyac yoxdur:** `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`, `Jwt:RefreshTokenDays`, `Email:Host`, `Email:Port`, `OpenRouter:Model`, `OpenRouter:OcrModel`, `GooglePlay:Products`.

---

## 3. Verilənlər bazası — PostgreSQL

Backend **PostgreSQL** (Npgsql provider) istifadə edir. Connection string formatı:
```
Host=<server>;Port=5432;Database=EssayCheckDb;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
```

**Hosting seçimləri:**

| Seçim | Qeyd |
|---|---|
| **Yerli/öz VPS-də Docker** | `docker-compose.dev.yml`-ə bənzər tərzdə, amma **mütləq named volume ilə** (data itməsin) və güclü parola ilə |
| **İdarə olunan bulud Postgres** (tövsiyə olunan) | Azure Database for PostgreSQL, AWS RDS for PostgreSQL, Neon, Supabase, Railway Postgres — hamısı standart Npgsql connection string ilə işləyir, backup/scaling özləri idarə edir |

**Vacib texniki qeyd (kodda artıq həll olunub, dəyişməyə ehtiyac yoxdur):** Bütün `DateTime` sütunları `EssayDbContext.OnModelCreating`-də açıq şəkildə `timestamp with time zone`-a map olunur, çünki tətbiqdə hər yerdə `DateTime.UtcNow` işlədilir və Npgsql-in defolt `timestamp without time zone` tipi UTC dəyərləri yazarkən istisna atır. Yeni `DateTime` sahə əlavə etsən, bu konvensiya avtomatik ona da tətbiq olunacaq.

---

## 4. Docker ilə Deploy

### 4.1. Image-i build et
```bash
docker build -t essaycheck-api:latest .
```

### 4.2. Konteyneri işə sal
```bash
docker run -d --name essaycheck-api -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=EssayCheckDb;Username=...;Password=...;SSL Mode=Require" \
  -e Jwt__Key="<32+ simvollu təsadüfi sətir>" \
  -e Email__SenderEmail="..." -e Email__Username="..." -e Email__Password="..." \
  -e OpenRouter__ApiKey="..." \
  -v /host/path/to/secrets:/app/secrets:ro \
  essaycheck-api:latest
```
> Google Play service account JSON-unu `-v` ilə **read-only volume** olaraq ver — heç vaxt image-in içinə qoyma.

### 4.3. Yoxla
```bash
curl http://localhost:8080/health
# → "Healthy"
```

Tətbiq başlayanda **avtomatik olaraq bütün EF Core migration-ları tətbiq edir** (boş bazadan belə) — əl ilə `dotnet ef database update` işlətməyə ehtiyac yoxdur.

---

## 5. Deploy sonrası smoke-test

```bash
# 1. Sağlamlıq
curl https://sənin-domenin/health

# 2. Qeydiyyat (test istifadəçisi)
curl -X POST https://sənin-domenin/api/auth/register -H "Content-Type: application/json" \
  -d '{"fullName":"Test","email":"test@example.com","password":"Test1234","confirmPassword":"Test1234","acceptTerms":true}'

# 3. Login
curl -X POST https://sənin-domenin/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234"}'
```
Tam endpoint siyahısı və nümunələr üçün → `POSTMAN_DOCS.md` (`baseUrl`-i real domenlə əvəz et).

---

## 6. Reverse Proxy / HTTPS

Tətbiq özü `app.UseHttpsRedirection()` çağırır, amma real HTTPS-i adətən **qarşıdakı proxy** (Nginx, Azure App Service, Cloudflare) idarə edir. `ForwardedHeaders` middleware-i artıq əlavə olunub — proxy `X-Forwarded-Proto`/`X-Forwarded-For` header-lərini göndərdiyi müddətcə düzgün işləyəcək (əksəriyyəti default olaraq göndərir).

---

## 7. Doğrulama (bu dəyişikliklər necə test edildi)

1. `docker build` ilə image uğurla yaradıldı.
2. Ayrıca, **sıfırdan** PostgreSQL 16 konteyneri qaldırıldı.
3. API konteyneri yalnız environment variable-larla (heç bir əl ilə migration olmadan) işə salındı.
4. Nəticə: **bütün 12 cədvəl avtomatik yaradıldı**, `GET /health` → `200 Healthy`, `POST /api/auth/register` → uğurlu qeydiyyat.
5. Əlavə olaraq yerli mühitdə (Postgres konteynerinə qarşı) tam axın test edildi: login (UTC `DateTime` yazılışı), refresh token rotasiyası, abunə yaradılması, tarixçə pagination və `jsonb` sütunların (Feedback/Mistakes) düzgün oxunub-yazılması — hamısı istisnasız işlədi.
6. Test konteynerləri/şəbəkəsi/image silindi (production-a təsiri yoxdur).

---

## 8. Hələ görülməli olan (bu sənədin əhatə etmədiyi)

- **Git-ə almaq** — repo hələ versiyalaşdırılmayıb. `.gitignore` artıq hazırdır, `git init` edəndə sirrlərin commit olunmayacağına əmin ol.
- **Real domen + SSL sertifikatı** (Let's Encrypt/Cloudflare) — hosting seçiminə görə fərqlənir.
- **CI/CD** — hazırda yoxdur, deploy əl ilə (`docker build` + `docker run`/`docker compose`) edilir.
- **Google Play məhsulları/RTDN** — bax `FRONTEND_GOOGLE_PLAY_BILLING.md`, bunlar Play Console-da ayrıca qurulmalıdır.
