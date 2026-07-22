# EssayCheck AI API — Postman Collection Sənədləşdirməsi

Bu sənəd `EssayCheck.postman_collection.json` faylındakı bütün endpoint-ləri ətraflı izah edir: nə üçün lazımdır, hansı sahələri qəbul edir, hansı cavabı qaytarır və hansı status kodları mümkündür.

---

## 1. Ümumi Məlumat

### 1.1. Collection-ı import etmək
1. Postman-ı aç → **Import** → `EssayCheck.postman_collection.json` faylını seç.
2. Collection **4 qovluqdan** (folder) ibarətdir: **Auth**, **Account**, **Essay**, **Subscription** — cəmi **23 sorğu**.

### 1.2. Dəyişənlər (Variables)
Collection səviyyəsində aşağıdakı dəyişənlər var (Postman-da **Collection → Variables** bölməsindən dəyişə bilərsən):

| Dəyişən | Defolt dəyər | Təyinatı |
|---|---|---|
| `baseUrl` | `https://localhost:7013` | API-nin ünvanı. HTTP üçün `http://localhost:5238` istifadə et. |
| `accessToken` | (boş) | JWT access token. **Login** və **Refresh Token** sorğuları uğurlu olduqda avtomatik doldurulur. |
| `refreshToken` | (boş) | Refresh token. **Login** və **Refresh Token** sorğularında avtomatik yenilənir. |
| `resetToken` | (boş) | Şifrə sıfırlama tokeni (e-mail-dən əl ilə kopyalanmalıdır — bax bölmə 3.6). |
| `essayId` | `1` | Son qiymətləndirilən essenin ID-si. **Evaluate Essay** sorğusu uğurlu olduqda avtomatik yenilənir. |
| `googleRtdnSecret` | (boş) | Google Play RTDN webhook-unun sirr açarı (bax bölmə 6.7). |

### 1.3. Autentifikasiya necə işləyir
- Collection səviyyəsində **Bearer Token** auth təyin olunub: `Authorization: Bearer {{accessToken}}`. Bütün sorğular bunu **miras alır** (inherit), yəni əl ilə token yapışdırmağa ehtiyac yoxdur.
- Auth tələb etməyən sorğularda (Register, Login, Refresh Token, Forgot/Reset Password, Get Plans, Google RTDN Webhook) **No Auth** açıq şəkildə təyin olunub.
- Axın belədir: **Auth → Login** işlət → `accessToken` və `refreshToken` avtomatik yaddaşa yazılır → bundan sonra bütün qorunan sorğular avtomatik işləyir.

### 1.4. SSL sertifikatı barədə qeyd
`localhost` üzərində ASP.NET Core-un **öz-özünü imzalayan (self-signed) development sertifikatı** işlədilir. Postman bunu etibarsız sayıb bağlana bilər. Həll: **Settings (⚙) → General → SSL certificate verification → OFF**.

### 1.5. Enum-ların JSON-da görünüşü
Backend-də bütün enum-lar **string** kimi seriallaşdırılır (rəqəm kimi deyil). Yəni sorğu/cavablarda məsələn `"plan": "ProPlus"` görünür, `"plan": 2` yox.

| Enum | Mümkün dəyərlər | Harada istifadə olunur |
|---|---|---|
| `SubscriptionPlan` | `Free`, `Pro`, `ProPlus` | Subscription cavabları, plan seçimi |
| `SubscriptionPlatform` | `Manual`, `GooglePlay`, `AppStore` | Abunəliyin mənbəyi |
| `EssayInputSource` | `Text`, `Image` | Esse hansı yolla göndərilib |
| `MistakeCategory` | `Grammar`, `Spelling`, `Vocabulary`, `NaturalExpression` | Səhv kateqoriyası (Qrammatika/Orfoqrafiya/Leksik/Təbii ifadə) |

### 1.6. Ümumi xəta formatları
API-da iki cür xəta cavabı var:

**A) Validasiya/məntiq xətaları** (əksər `BadRequest` cavabları) — sadə obyekt:
```json
{ "message": "İzahlı xəta mesajı burada" }
```

**B) Model validasiyası uğursuz olduqda** (ASP.NET Core-un avtomatik `[ApiController]` davranışı) — `ProblemDetails` formatı:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["E-mail ünvanı düzgün deyil."]
  }
}
```

**C) Gözlənilməz server xətaları** — Global Exception Handler tərəfindən tutulur:
```json
{ "message": "Gözlənilməz xəta baş verdi." }
```
- AI xidməti (OpenRouter) müvəqqəti əlçatmazdırsa: **503** + `"AI xidməti hazırda məşğuldur, bir azdan yenidən cəhd edin."`
- AI xidməti ilə əlaqə tam kəsilibsə: **502** + `"AI xidməti ilə əlaqə alınmadı."`

**D) Register/Login/ForgotPassword/ResetPassword/Account əməliyyatları** — `AuthResult` formatı:
```json
{ "succeeded": true, "message": "Qeydiyyat uğurla tamamlandı.", "errors": [] }
```
uğursuz olduqda:
```json
{ "succeeded": false, "message": "", "errors": ["Bu e-mail ünvanı artıq qeydiyyatdan keçib."] }
```

---

## 2. Qovluq: **Auth** (7 sorğu)

Baza yol: `/api/auth`. Bu qovluqdakı bütün endpoint-lər (Logout və Get Profile istisna olmaqla) **açıqdır** (token tələb etmir).

### 2.1. Register — `POST /api/auth/register`
Yeni istifadəçi qeydiyyatı.

**Auth:** Yoxdur (No Auth)

**Body (JSON):**
| Sahə | Tip | Tələblər |
|---|---|---|
| `fullName` | string | Boş olmamalı, maks. 100 simvol |
| `email` | string | Boş olmamalı, düzgün e-mail formatı |
| `password` | string | Boş olmamalı, ən azı 8 simvol, böyük+kiçik hərf+rəqəm |
| `confirmPassword` | string | `password` ilə eyni olmalı |
| `acceptTerms` | bool | `true` olmalı (əks halda rədd edilir) |

**Uğurlu cavab (200):**
```json
{ "succeeded": true, "message": "Qeydiyyat uğurla tamamlandı.", "errors": [] }
```

**Status kodları:**
- `200` — uğurlu qeydiyyat
- `400` — email artıq mövcuddur / `acceptTerms=false` / şifrə qaydalarına uyğun deyil / model validasiyası

---

### 2.2. Login — `POST /api/auth/login`
Giriş. Uğurlu olduqda JWT access token + refresh token qaytarır.

**Auth:** Yoxdur

**Body:**
| Sahə | Tip | Tələblər |
|---|---|---|
| `email` | string | Boş olmamalı, düzgün format |
| `password` | string | Boş olmamalı |

**Uğurlu cavab (200):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "base64-random-64-byte-string...",
  "expiresAt": "2026-07-19T18:30:00Z",
  "fullName": "Faig Rəsulzadə",
  "email": "faig@example.com"
}
```

> **Postman avtomatizasiyası:** Bu sorğunun "Tests" skripti `token` və `refreshToken`-i avtomatik `accessToken`/`refreshToken` collection dəyişənlərinə yazır.

**JWT parametrləri:** access token `Jwt:ExpiryMinutes` (defolt 120 dəqiqə) sonra bitir; refresh token `Jwt:RefreshTokenDays` (defolt 30 gün) etibarlıdır.

**Status kodları:**
- `200` — uğurlu giriş
- `401` — `{"message":"E-mail və ya şifrə yanlışdır."}` (yanlış məlumat, ya da hesab silinib)

---

### 2.3. Refresh Token — `POST /api/auth/refresh`
Access token bitəndə (və ya bitmədən əvvəl) yeni access + refresh token cütü almaq üçün.

**Auth:** Yoxdur (refresh token özü autentifikasiya rolunu oynayır)

**Body:**
| Sahə | Tip | Tələblər |
|---|---|---|
| `refreshToken` | string | Boş olmamalı |

**Necə işləyir (Rotation təhlükəsizlik modeli):**
- Göndərilən refresh token yoxlanılır (hash-lənmiş formada bazada saxlanılır, `SHA-256`).
- Etibarlıdırsa: **köhnə token dərhal ləğv edilir (revoke)** və **yeni cüt** (access + refresh) yaradılır.
- Eyni refresh token-i **ikinci dəfə** işlətmək mümkün deyil — bu, oğurlanmış token-in aşkarlanması üçün standart təhlükəsizlik metodudur.

**Uğurlu cavab (200):** Login ilə eyni format (`token`, `refreshToken`, `expiresAt`, `fullName`, `email`).

> **Postman avtomatizasiyası:** Bu sorğu da uğurlu olduqda `accessToken`/`refreshToken` dəyişənlərini avtomatik yeniləyir.

**Status kodları:**
- `200` — uğurlu yeniləmə
- `401` — `{"message":"Refresh token etibarsız və ya vaxtı bitib."}` (artıq işlədilib, vaxtı bitib, və ya mövcud deyil)

---

### 2.4. Logout — `POST /api/auth/logout`
Cari sessiyanı bağlayır (refresh token-i ləğv edir).

**Auth:** **Tələb olunur** (Bearer access token)

**Body:**
| Sahə | Tip |
|---|---|
| `refreshToken` | string |

**Cavab:** `204 No Content` (body yoxdur)

**Qeyd:** Access token JWT olduğu üçün (stateless) logout-dan sonra da öz müddəti bitənə qədər texniki işlək qalır, amma **refresh** artıq mümkün olmayacaq — beləliklə sessiya faktiki bağlanır.

---

### 2.5. Forgot Password — `POST /api/auth/forgot-password`
E-mail ünvanına şifrə sıfırlama linki göndərir.

**Auth:** Yoxdur

**Body:**
| Sahə | Tip | Tələblər |
|---|---|---|
| `email` | string | Boş olmamalı, düzgün format |

**Uğurlu cavab (200) — HƏMİŞƏ eyni mesaj (təhlükəsizlik üçün):**
```json
{ "succeeded": true, "message": "Əgər e-mail ünvanı sistemdə mövcuddursa, şifrə sıfırlama linki göndərildi.", "errors": [] }
```

> **Qeyd:** E-mail sistemdə olsun-olmasın cavab **eyni** olur — bu, "hansı e-mail-lər qeydiyyatdadır" məlumatının sızmasının qarşısını alır. Real e-mail göndərilibsə, link daxilində `email` və `token` query parametrləri var (`App:ResetPasswordUrl?email=...&token=...`). Test üçün həmin `token`-i e-maildən götürüb `resetToken` dəyişəninə yapışdır.

---

### 2.6. Reset Password — `POST /api/auth/reset-password`
E-maildəki token ilə yeni şifrə təyin edir.

**Auth:** Yoxdur

**Body:**
| Sahə | Tip | Tələblər |
|---|---|---|
| `email` | string | Qeydiyyatdakı email |
| `token` | string | E-mail-dən gələn sıfırlama tokeni (`{{resetToken}}`) |
| `newPassword` | string | Ən azı 8 simvol |
| `confirmPassword` | string | `newPassword` ilə eyni |

**Uğurlu cavab (200):**
```json
{ "succeeded": true, "message": "Şifrəniz uğurla dəyişdirildi.", "errors": [] }
```
Bu əməliyyatdan sonra **bütün aktiv refresh token-lər ləğv olunur** — istifadəçi bütün cihazlardan yenidən login etməli olur. Həmçinin e-mail-ə "şifrəniz dəyişdirildi" bildirişi göndərilir.

**Status kodları:**
- `200` — uğurlu
- `400` — token etibarsız/bitib, ya da istifadəçi tapılmadı

---

### 2.7. Get Profile — `GET /api/auth/profile`
Cari istifadəçinin profil məlumatı.

**Auth:** **Tələb olunur**

**Uğurlu cavab (200):**
```json
{
  "id": 12,
  "fullName": "Faig Rəsulzadə",
  "email": "faig@example.com",
  "createdAt": "2026-06-01T10:00:00Z",
  "lastLoginDate": "2026-07-19T09:15:00Z"
}
```

**Status kodları:**
- `200` — uğurlu
- `401` — token yoxdur/etibarsızdır
- `404` — istifadəçi tapılmadı (silinib)

---

## 3. Qovluq: **Account** (3 sorğu)

Baza yol: `/api/account`. **Bütün endpoint-lər autentifikasiya tələb edir.** Bunlar "Ayarlar" ekranına uyğundur.

### 3.1. Update Profile — `PUT /api/account/profile`
Ad/soyadı yeniləyir.

**Body:**
| Sahə | Tip | Tələblər |
|---|---|---|
| `fullName` | string | Boş olmamalı, maks. 100 simvol |

**Cavab (200):** `AuthResult` — `{ "succeeded": true, "message": "Profil yeniləndi.", "errors": [] }`

---

### 3.2. Change Password — `PUT /api/account/password`
Autentifikasiyalı istifadəçi cari şifrəsini bilərək yeni şifrə təyin edir.

**Body:**
| Sahə | Tip | Tələblər |
|---|---|---|
| `currentPassword` | string | Doğru olmalı |
| `newPassword` | string | Ən azı 8 simvol |
| `confirmPassword` | string | `newPassword` ilə eyni |

**Cavab (200):** `{ "succeeded": true, "message": "Şifrə uğurla dəyişdirildi.", "errors": [] }`

**Qeyd:** Uğurlu şifrə dəyişikliyindən sonra **bütün refresh token-lər ləğv olunur** (bütün cihazlarda yenidən login lazımdır).

**Status kodları:**
- `200` — uğurlu
- `400` — cari şifrə yanlışdır, ya da yeni şifrə qaydalara uyğun deyil

---

### 3.3. Delete Account (soft) — `DELETE /api/account`
Hesabı silir — **soft delete** (məlumat bazada qalır, `IsDeleted=true` olur).

**Body:** Yoxdur

**Cavab (200):** `{ "succeeded": true, "message": "Hesab silindi.", "errors": [] }`

**Nə baş verir:**
- İstifadəçi `IsDeleted=true` işarələnir (bundan sonra login/refresh mümkün deyil).
- Bütün aktiv refresh token-lər ləğv olunur.
- Aktiv abunəlik(lər) deaktiv edilir.
- Essay tarixçəsi **silinmir** (yalnız hesaba giriş bağlanır).

---

## 4. Qovluq: **Essay** (6 sorğu)

Baza yol: `/api/essay`. **Bütün endpoint-lər autentifikasiya tələb edir.**

### 4.1. Evaluate Essay — `POST /api/essay/evaluate`
Əsas funksiya: esse mətnini süni intellektlə (OpenRouter AI, DİM meyarlarına görə) qiymətləndirir və nəticəni tarixçəyə yazır.

**Body:**
| Sahə | Tip | Tələblər |
|---|---|---|
| `text` | string | Boş olmamalı, maks. 5000 simvol |
| `title` | string? | Opsional; boş olsa mətnin ilk sətrindən avtomatik yaradılır (maks. 60 simvol + "…") |
| `source` | enum | `Text` (defolt) və ya `Image` — mətnin haradan gəldiyini bildirir (OCR-dan sonra göndərilirsə `Image` yaz) |

**Əməliyyat ardıcıllığı (backend daxilində):**
1. **Gündəlik limit yoxlanılır** (Free plan üçün gündə 1 pulsuz yoxlama). Limit dolubsa AI-a müraciət **edilmir**, dərhal `429` qaytarılır.
2. Mətn OpenRouter AI-a göndərilir, DİM meyarlarına görə qiymətləndirilir.
3. Əgər AI mətni **esse hesab etmirsə** (məs. mənasız mətn, tamam başqa mövzu) — `422` qaytarılır, tarixçəyə **yazılmır**, limit **azalmır**.
4. Uğurlu olduqda nəticə `Essays` cədvəlinə yazılır və gündəlik sayğac yalnız **bundan sonra** artırılır.

**Uğurlu cavab (200) — tam nümunə:**
```json
{
  "id": 42,
  "title": "Shopping",
  "createdAt": "2026-07-19T12:00:00Z",
  "source": "Text",
  "wordCount": 34,
  "accuracyPercent": 74,
  "totalScore": 3.7,
  "correctedEssay": "Nowadays shopping plays an important role... <b>go to shopping</b> (go shopping) ...",
  "statistics": {
    "grammar": 4,
    "spelling": 1,
    "vocabulary": 0,
    "naturalExpression": 3,
    "total": 8
  },
  "mistakes": [
    {
      "wrong": "go to shopping",
      "correct": "go shopping",
      "category": "Grammar",
      "reason": "\"Go shopping\" fe'li birləşməsi ilə istifadə olunur, \"to\" əlavə olunmur."
    }
  ],
  "scores": {
    "structure": 0.9,
    "content": 1.5,
    "grammar": 0.6,
    "vocabulary": 0.7,
    "total": 3.7
  },
  "feedback": {
    "strengths": ["Giriş və nəticə aydındır."],
    "weaknesses": ["Qrammatika səhvlərinə diqqət yetirin."],
    "recommendations": ["Fikirlərinizi daha ətraflı inkişaf etdirin."]
  }
}
```

**Sahələrin izahı:**
- `accuracyPercent` = `(totalScore / 5) * 100` (yuvarlaqlaşdırılmış)
- `statistics` — səhv sayları kateqoriya üzrə (`total` = digər 4-ün cəmi)
- `scores` — DİM meyarları: `structure` (0–1), `content` (0–2), `grammar` (0–1), `vocabulary` (0–1), `total` (maks. 5)
- `mistakes[].category` — `Grammar` | `Spelling` | `Vocabulary` | `NaturalExpression`

> **Postman avtomatizasiyası:** Uğurlu cavabdan `id` avtomatik `essayId` dəyişəninə yazılır ki, "History Detail"/"Delete History Item" sorğuları dərhal işləsin.

**Status kodları:**
- `200` — uğurlu qiymətləndirmə
- `422` — `{"message":"..."}` — göndərilən mətn esse deyil (AI-ın qərarı)
- `429` — `{"message":"Bugünkü pulsuz limit (1) bitib. Sabah yenilənəcək və ya Pro planına keçin."}` — Free plan gündəlik limiti dolub
- `502` / `503` — AI xidməti ilə bağlı müvəqqəti problem (yenidən cəhd et)

---

### 4.2. OCR (image → text) [Pro Plus] — `POST /api/essay/ocr`
Şəkildəki əl yazısı/mətni oxuyub sadəcə **mətn olaraq qaytarır** (tarixçəyə yazmır, qiymətləndirmir). İstifadəçi qaytarılan mətnə baxıb lazım olsa düzəliş edir, sonra **Evaluate Essay**-ə göndərir.

**Body:** `form-data`
| Sahə | Tip | Tələblər |
|---|---|---|
| `image` | file | Şəkil faylı (`image/*` MIME tipi), maksimum 10 MB |

**Plan tələbi:** Yalnız **Pro Plus** istifadəçiləri istifadə edə bilər (Free və Pro üçün `403`).

**Uğurlu cavab (200):**
```json
{ "text": "Nowadays shopping plays an important role in everybody's life..." }
```

**Status kodları:**
- `200` — uğurlu OCR
- `400` — şəkil göndərilməyib, ya da fayl şəkil formatında deyil
- `403` — `{"message":"Şəkildən esse oxuma yalnız Pro Plus üçün əlçatandır."}` (Free/Pro plan)

**Qeyd:** Uğurlu OCR-dan sonra **OCR gündəlik sayğacı** artırılır (Pro Plus üçün limitsiz olsa da sayğac saxlanılır).

---

### 4.3. History (list) — `GET /api/essay/history`
İstifadəçinin bütün esse tarixçəsini (yüngül siyahı formatında) qaytarır.

**Query parametrləri:**
| Parametr | Tip | Tələblər |
|---|---|---|
| `search` | string? | Opsional — başlıqda (title) axtarış |

**Uğurlu cavab (200):**
```json
{
  "items": [
    { "id": 42, "title": "Shopping", "createdAt": "2026-07-19T12:00:00Z", "wordCount": 34, "totalScore": 3.7 },
    { "id": 41, "title": "My Future Career", "createdAt": "2026-07-14T18:42:00Z", "wordCount": 95, "totalScore": 2.8 }
  ],
  "totalCount": 2,
  "averageScore": 3.3
}
```
Siyahı **ən yenidən köhnəyə** sıralanır. `averageScore` bütün (və ya axtarış nəticəsindəki) esselərin ortalama balıdır.

---

### 4.4. History Detail — `GET /api/essay/history/{id}`
Bir esse qeydinin **tam detalını** qaytarır (Evaluate Essay-in cavabı ilə eyni format — bax 4.1).

**URL parametri:** `{{essayId}}` (Postman-da avtomatik doldurulur)

**Status kodları:**
- `200` — uğurlu
- `404` — bu ID-yə uyğun qeyd tapılmadı (ya mövcud deyil, ya başqa istifadəçiyə aiddir)

---

### 4.5. Delete History Item — `DELETE /api/essay/history/{id}`
Tək bir tarixçə qeydini silir.

**Cavab:** `204 No Content` (uğurlu) / `404` (tapılmadı)

---

### 4.6. Delete All History — `DELETE /api/essay/history`
İstifadəçinin **bütün** tarixçəsini silir ("Ayarlar → Tarixçəni sil" funksiyası).

**Uğurlu cavab (200):**
```json
{ "deleted": 6 }
```
`deleted` — silinən qeydlərin sayı.

---

## 5. Qovluq: **Subscription** (7 sorğu)

Baza yol: `/api/subscription`. **Get Plans** və **Google Play RTDN Webhook** istisna olmaqla bütün endpoint-lər autentifikasiya tələb edir.

### 5.1. Get Plans (public) — `GET /api/subscription/plans`
Bütün planların kataloqunu qaytarır ("Planlar" ekranı üçün). **Token tələb etmir.**

**Uğurlu cavab (200):**
```json
[
  {
    "plan": "Free", "name": "Free", "price": 0, "currency": "AZN", "period": "ay",
    "unlimitedText": false, "dailyTextLimit": 1, "ocr": false,
    "features": ["Gündə 1 esse şansı (mətnlə yaz)", "Tarixçə (pulsuz)"]
  },
  {
    "plan": "Pro", "name": "Pro", "price": 4.99, "currency": "AZN", "period": "ay",
    "unlimitedText": true, "dailyTextLimit": null, "ocr": false,
    "features": ["Limitsiz esse (mətnlə yaz)", "Tarixçə (pulsuz)"]
  },
  {
    "plan": "ProPlus", "name": "Pro Plus", "price": 9.99, "currency": "AZN", "period": "ay",
    "unlimitedText": true, "dailyTextLimit": null, "ocr": true,
    "features": ["Limitsiz esse (mətnlə yaz)", "Limitsiz esse (şəkildən oxu)", "Tarixçə (pulsuz)"]
  }
]
```

---

### 5.2. My Subscription — `GET /api/subscription`
Cari istifadəçinin aktiv abunəliyini qaytarır.

**Uğurlu cavab (200) — Free (abunəlik yoxdursa):**
```json
{ "plan": "Free", "isActive": true, "startDate": null, "endDate": null, "autoRenew": false, "platform": null }
```

**Aktiv ödənişli abunəlik varsa:**
```json
{ "plan": "ProPlus", "isActive": true, "startDate": "2026-07-19T04:31:50Z", "endDate": "2026-08-18T04:31:50Z", "autoRenew": true, "platform": "Manual" }
```

**Qeyd:** Əgər `endDate` keçmişdə qalıbsa, sistem **avtomatik olaraq** abunəliyi deaktiv edir və istifadəçi Free plana düşür (bu yoxlama hər çağırışda aparılır).

---

### 5.3. Subscribe (Pro / Pro Plus) — `POST /api/subscription/subscribe`
**Manual/test məqsədli** plan keçidi (admin panel və ya test ssenariləri üçün). **Real Google Play alışları üçün bunun əvəzinə bölmə 5.6-dakı `google/verify` istifadə olunmalıdır.**

**Body:**
| Sahə | Tip | Tələblər |
|---|---|---|
| `plan` | enum | `Pro` və ya `ProPlus` (`Free` göndərilə bilməz) |
| `platform` | enum | `Manual` (defolt), `GooglePlay`, `AppStore` |
| `purchaseToken` | string? | Opsional, maks. 4000 simvol |
| `autoRenew` | bool | Defolt `false` |
| `durationDays` | int | 1–366 aralığında, defolt 30 |

**Uğurlu cavab (200):** "My Subscription" ilə eyni format, `startDate=indi`, `endDate=indi+durationDays`.

**Status kodları:**
- `200` — uğurlu
- `400` — `plan=Free` göndərilib (`{"message":"Free plana abunə olmaq lazım deyil. Ləğv üçün /cancel istifadə edin."}`)

---

### 5.4. Cancel Subscription — `POST /api/subscription/cancel`
Aktiv abunəliyi ləğv edir, istifadəçi Free plana düşür.

**Body:** Yoxdur

**Cavab (200):** Free cavab formatı (bax 5.2).

---

### 5.5. Usage Status — `GET /api/subscription/usage`
"Əsas səhifə"dəki **"Gündəlik pulsuz şans"** blokunun backend datası.

**Uğurlu cavab (200) — Free plan, hələ istifadə edilməyib:**
```json
{
  "plan": "Free",
  "unlimitedText": false,
  "dailyTextLimit": 1,
  "textUsedToday": 0,
  "textRemaining": 1,
  "canUseOcr": false,
  "resetAtUtc": "2026-07-20T00:00:00Z"
}
```
**Pro/Pro Plus üçün** `unlimitedText=true`, `dailyTextLimit=null`, `textRemaining=null`.

**Sahələrin izahı:**
- `resetAtUtc` — limitin sıfırlanacağı vaxt (həmişə **UTC gecə yarısı**, gündəlik limit UTC tarixinə görə hesablanır).
- `textUsedToday` — bugün (UTC) uğurla qiymətləndirilmiş esse sayı.

---

### 5.6. Google Play — Verify Purchase — `POST /api/subscription/google/verify`
**Real Google Play Billing inteqrasiyası.** Mobil tətbiq Google Play SDK vasitəsilə satınalma etdikdən sonra alınan `productId` + `purchaseToken`-i backend-ə göndərir. Backend bu məlumatı **birbaşa Google Play Developer API-yə göndərib doğrulayır** — client-in dediyinə etibar edilmir.

**Body:**
| Sahə | Tip | Tələblər |
|---|---|---|
| `productId` | string | Google Play Console-da təyin olunan subscription ID (məs. `pro_plus_monthly`) |
| `purchaseToken` | string | Google Play SDK-nın satınalmadan sonra qaytardığı token |

**Backend daxilində nə baş verir:**
1. `productId` `appsettings.json → GooglePlay:Products` xəritəsində axtarılır → daxili `SubscriptionPlan`-a çevrilir. Tapılmasa `400`.
2. Google Play Developer API-yə sorğu göndərilir (`purchases.subscriptions.get`) — satınalmanın **aktivliyi, bitmə tarixi, avtomatik yenilənməsi** yoxlanılır.
3. Satınalma aktiv deyilsə (bitib/ləğv edilib) → `400`.
4. Eyni `purchaseToken` **başqa istifadəçiyə** aiddirsə (fırıldaq/paylaşılan token qarşısı) → `400`.
5. Google tərəfindən hələ "acknowledge" edilməyibsə, backend avtomatik acknowledge edir (Google 3 gün ərzində acknowledge tələb edir, yoxsa avtomatik pul geri qaytarır).
6. İstifadəçinin əvvəlki aktiv abunəlikləri deaktiv edilir, yeni abunəlik `Platform=GooglePlay` ilə yaradılır/yenilənir.

**Uğurlu cavab (200):** "My Subscription" ilə eyni format, `platform="GooglePlay"`.

**Status kodları:**
- `200` — uğurlu təsdiq
- `400` — bir neçə səbəbdən ola bilər:
  - `{"message":"Naməlum Google Play məhsulu: ..."}` — `productId` `GooglePlay:Products`-da tanınmır
  - `{"message":"Satınalma aktiv deyil (bitib və ya ləğv edilib)."}`
  - `{"message":"Bu satınalma artıq başqa hesaba bağlıdır."}`
  - `{"message":"Google Play Billing hələ konfiqurasiya edilməyib (...)."}` — backend-də `GooglePlay` bölməsi hələ doldurulmayıb (bax bölmə 6)
  - `{"message":"Google Play satınalması təsdiqlənmədi: ..."}` — Google API-dən xəta (etibarsız token və s.)

> **Qeyd:** Bu endpoint hazırda **Google Play Console tam konfiqurasiya olunana qədər** `400` qaytaracaq ("hələ konfiqurasiya edilməyib" mesajı ilə) — bu, gözlənilən davranışdır, backend xətası deyil.

---

### 5.7. Google Play — RTDN Webhook (Pub/Sub push) — `POST /api/subscription/google/rtdn`
**Bu endpoint istifadəçi tərəfindən deyil, Google Cloud Pub/Sub tərəfindən avtomatik çağırılır.** Məqsədi: abunəlik yenilənəndə/ləğv olunanda/bitəndə (istifadəçi tətbiqi açmasa belə) backend-in bundan **dərhal xəbərdar olması**.

**Auth:** Token yoxdur, əvəzinə **query string sirri**: `?secret={{googleRtdnSecret}}`. Bu sirr `appsettings.json → GooglePlay:RtdnSharedSecret` ilə eyni olmalıdır, əks halda `401`.

**Body (Google Pub/Sub-un standart formatı):**
```json
{
  "message": {
    "data": "<base64 kodlanmış RTDN JSON payload-ı>",
    "messageId": "123456",
    "publishTime": "2026-07-19T00:00:00Z"
  },
  "subscription": "projects/your-project/subscriptions/your-sub"
}
```
`data` sahəsi base64-dən açılanda belə görünür:
```json
{
  "packageName": "az.essaycheck.app",
  "eventTimeMillis": "1234567890",
  "subscriptionNotification": {
    "notificationType": 4,
    "purchaseToken": "...",
    "subscriptionId": "pro_plus_monthly"
  }
}
```

**Backend daxilində nə baş verir:**
1. `messageId` yoxlanılır — **eyni mesaj iki dəfə emal olunmur** (Pub/Sub "at-least-once" çatdırdığı üçün təkrarlar mümkündür, idempotentlik `ProcessedGoogleNotifications` cədvəli ilə təmin olunur).
2. `notificationType=12` (REVOKED) olduqda abunəlik **dərhal deaktiv** edilir.
3. Digər bütün növlərdə (RENEWED, CANCELED, EXPIRED, PURCHASED, RESTARTED və s.) backend Google API-yə **yenidən müraciət edib** cari vəziyyəti (bitmə tarixi, aktivlik) təsdiqləyir və bazanı ona uyğun yeniləyir.

**Cavab:** Həmişə `200 OK` (uğurla emal olunsa da, daxili xəta baş versə də — Google-un təkrar-təkrar cəhd etməsinin (retry storm) qarşısını almaq üçün xətalar loglanır, amma 200 qaytarılır). Yalnız sirr yanlış/yoxdursa `401` qaytarılır.

**Qeyd:** Bu sorğunu Postman-dan əl ilə test etmək real ssenari deyil — real mühitdə Google Play Console-un RTDN Pub/Sub push subscription-ı bu URL-ə yönləndirilir. Postman-dan test yalnız **sirr yoxlamasının işlədiyini** (401/200) yoxlamaq üçündür.

---

## 6. Google Play Billing — Backend Konfiqurasiyası (İstehsalata keçmək üçün)

`google/verify` və `google/rtdn` endpoint-ləri işləməzdən əvvəl backend-də (`appsettings.json` və ya `user-secrets`) aşağıdakılar doldurulmalıdır:

```json
"GooglePlay": {
  "PackageName": "az.essaycheck.app",
  "ServiceAccountJsonPath": "secrets/google-play-service-account.json",
  "ApplicationName": "EssayCheck AI",
  "Products": {
    "pro_monthly": "Pro",
    "pro_plus_monthly": "ProPlus"
  },
  "RtdnSharedSecret": "uzun-təsadüfi-gizli-sətir"
}
```

| Sahə | Haradan alınır |
|---|---|
| `PackageName` | Google Play Console → tətbiqin package name-i |
| `ServiceAccountJsonPath` | Google Cloud → Service Account yaradıb JSON açarını endirmək, Play Console-da "Financial data" icazəsi vermək |
| `Products` | Play Console-da yaratdığın subscription ID-lərinin daxili plana uyğunlaşdırması |
| `RtdnSharedSecret` | Özün təyin etdiyin təsadüfi sətir (ən azı 16+ simvol tövsiyə olunur) — Pub/Sub push subscription-ın URL-inə `?secret=...` kimi əlavə olunmalıdır |

> **Qeyd:** Bu bölmə **qəsdən** sərt validasiyaya tabe deyil — Google Play tam qurulmayana qədər boş qalsa belə, **API-nin qalan hissəsi (Auth, Essay, digər Subscription endpoint-ləri) normal işləyir**. Yalnız `google/verify` və `google/rtdn` çağırıldıqda konfiqurasiyanın tam olub-olmadığı yoxlanılır.

---

## 7. Tövsiyə olunan test ardıcıllığı

1. **Auth → Register** → yeni istifadəçi yarat
2. **Auth → Login** → token-lər avtomatik yadda saxlanılır
3. **Subscription → Get Plans** → planları gör
4. **Subscription → Usage Status** → Free limitini gör (`textRemaining: 1`)
5. **Essay → Evaluate Essay** → esse göndər, nəticəyə bax (`essayId` avtomatik yadda saxlanılır)
6. **Subscription → Usage Status** → təkrar çağır, `textRemaining: 0` olduğunu gör
7. **Essay → Evaluate Essay** → təkrar göndər → **429** al (gündəlik limit)
8. **Subscription → Subscribe (Pro / Pro Plus)** → `ProPlus`-a keç
9. **Essay → OCR** → indi icazə var (`canUseOcr=true`)
10. **Essay → History (list)** / **History Detail** → nəticələrə bax
11. **Account → Update Profile** / **Change Password** → ayarları test et
12. **Auth → Refresh Token** → köhnə `refreshToken`-in artıq işləmədiyini yoxla (təkrar çağırsan 401 alacaqsan)
13. **Auth → Logout** → sessiyanı bağla
