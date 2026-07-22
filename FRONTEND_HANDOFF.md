# EssayCheck AI — Frontend Handoff Sənədi (React Native + TypeScript)

Bu sənəd EssayCheck AI-nin **.NET 8 backend-inin tam API kontraktını** və frontend-in bilməli olduğu bütün biznes qaydalarını əhatə edir. Backend **tamamilə hazır və test edilib**; bu sənəd yeni bir söhbətdə (Zed + Claude Code, React Native/TypeScript, Android Studio) frontend inkişafına başlamaq üçün lazım olan bütün konteksti verir.

> **Əlavə dərin sənədlər** (backend repo-sunda mövcuddur, lazım olsa əlavə at): `POSTMAN_DOCS.md` (hər endpoint üçün tam JSON nümunələri + xəta halları), `FRONTEND_GOOGLE_PLAY_BILLING.md` (react-native-iap inteqrasiyası addım-addım).

---

## 1. Tətbiq nədir

**EssayCheck AI** — Azərbaycan **DİM** (Dövlət İmtahan Mərkəzi) meyarlarına görə ingilis dilində yazılmış esseləri süni intellektlə qiymətləndirən mobil tətbiq.

**Əsas axın:** istifadəçi esse yazır (və ya şəkildən oxutdurur) → AI qiymətləndirir (DİM meyarları, qrammatika/orfoqrafiya/leksik/təbii ifadə səhvləri, ümumi bal /5) → nəticə tarixçəyə yazılır.

**Planlar:** Free (gündə 1 pulsuz mətn yoxlaması, OCR yox) / Pro (limitsiz mətn, OCR yox) / Pro Plus (limitsiz mətn + limitsiz OCR).

---

## 2. Ekranlar → Backend Uyğunlaşması

Dizayn (Figma) hazırdır, aşağıdakı 10 ekranı əhatə edir:

| Ekran | Backend endpoint(lər)i |
|---|---|
| **Daxil ol (Login)** | `POST /api/auth/login` |
| **Qeydiyyat (Register)** | `POST /api/auth/register` |
| **Şifrəni unutmusunuz** | `POST /api/auth/forgot-password` |
| **Yeni şifrə yarat** | `POST /api/auth/reset-password` |
| **Əsas səhifə** (esse yaz + gündəlik şans + cari plan) | `POST /api/essay/evaluate`, `GET /api/subscription/usage`, `GET /api/subscription` |
| **Şəkildən oxu** (Əsas səhifədə tab, Pro Plus) | `POST /api/essay/ocr` → nəticəni `evaluate`-ə göndər |
| **Tarixçə** (siyahı, axtarış) | `GET /api/essay/history?search=&page=&pageSize=` |
| **Tarixçə detalı** (2 ekran: düzəliş+statistika, DİM meyarları+rəy) | `GET /api/essay/history/{id}`, `DELETE /api/essay/history/{id}` |
| **Planlar** | `GET /api/subscription/plans`, `POST /api/subscription/subscribe` (test) / `POST /api/subscription/google/verify` (real) |
| **Ayarlar** | `GET /api/auth/profile`, `PUT /api/account/profile`, `PUT /api/account/password`, `DELETE /api/essay/history`, `DELETE /api/account`, `POST /api/auth/logout` |

**Hazırda backend-də YOXDUR (UI-da düymə göstərilə bilər, amma funksional deyil):** Google/Apple ilə sosial giriş, App Store (iOS) billing.

---

## 3. Autentifikasiya modeli (JWT + Refresh Token)

- **Access token** (`token`): JWT, `Authorization: Bearer <token>` header-i ilə göndərilir. Ömrü **~120 dəqiqə**.
- **Refresh token**: təsadüfi sətir (JWT deyil), ömrü **~30 gün**. **Rotasiya** olunur — hər `refresh` çağırışında köhnəsi etibarsız olur, yenisi verilir. **Köhnə refresh token-i saxlama, dərhal yenisi ilə əvəz et.**
- **Saxlama:** access + refresh token-ləri **təhlükəsiz storage**-da saxla (`react-native-keychain` və ya `expo-secure-store` — sadə `AsyncStorage` YOX, çünki şifrələnməmişdir).
- **401 aldıqda:** bir dəfə `POST /api/auth/refresh` ilə yenilə, uğursuz olarsa istifadəçini login ekranına yönləndir (tokenləri təmizlə).
- **423 (Locked) aldıqda** (login zamanı): hesab 5 yanlış cəhddən sonra 15 dəqiqə bloklanır — cavabda `lockoutEndsAt` gəlir, bunu göstər ("X dəqiqə sonra yenidən cəhd edin").
- **Logout:** `POST /api/auth/logout` (body: `refreshToken`) çağır, sonra local storage-dan hər iki tokeni sil.

---

## 4. Tam API Kontraktı

**Base URL (dev):** `https://localhost:7013` (fiziki cihazda test edərkən `10.0.2.2` (Android emulator) və ya kompüterin LAN IP-si istifadə olunmalıdır, `localhost` yox).

Bütün enum-lar JSON-da **string** kimi gəlir (məs. `"plan": "ProPlus"`, rəqəm yox).

### 4.1. Auth (`/api/auth`)

| Method + Path | Auth | Body | Cavab (200) | Xəta halları |
|---|---|---|---|---|
| `POST /register` | ✗ | `{fullName, email, password, confirmPassword, acceptTerms}` | `{succeeded, message, errors[]}` | `400` |
| `POST /login` | ✗ | `{email, password}` | `{token, refreshToken, expiresAt, fullName, email}` | `401` yanlış, `423` lockout (+`lockoutEndsAt`) |
| `POST /refresh` | ✗ | `{refreshToken}` | eyni format `login` ilə | `401` etibarsız/bitib |
| `POST /logout` | ✓ | `{refreshToken}` | `204` | — |
| `POST /forgot-password` | ✗ | `{email}` | `{succeeded, message, errors[]}` (həmişə uğur mesajı) | — |
| `POST /reset-password` | ✗ | `{email, token, newPassword, confirmPassword}` | `{succeeded, message, errors[]}` | `400` |
| `GET /profile` | ✓ | — | `{id, fullName, email, createdAt, lastLoginDate}` | `401`, `404` |

### 4.2. Account (`/api/account`) — hamısı `✓` auth tələb edir

| Method + Path | Body | Cavab | Xəta |
|---|---|---|---|
| `PUT /profile` | `{fullName}` | `{succeeded, message, errors[]}` | `400` |
| `PUT /password` | `{currentPassword, newPassword, confirmPassword}` | eyni | `400` (yanlış cari şifrə) |
| `DELETE /` (hesabı sil, soft delete) | — | eyni | `400` |

### 4.3. Essay (`/api/essay`) — hamısı `✓` auth tələb edir

| Method + Path | Body/Query | Cavab (200) | Xəta halları |
|---|---|---|---|
| `POST /evaluate` | `{text, title?, source: "Text"|"Image"}` (text maks. 5000 simvol) | `EssayDetailResponse` (aşağıda) | `422` AI mətni esse saymır, `429` gündəlik limit bitib, `502/503` AI müvəqqəti xəta |
| `POST /ocr` | form-data `image` (maks 10MB) | `{text}` | `403` yalnız Pro Plus, `400` şəkil deyil |
| `GET /history` | query: `search?`, `page?` (defolt 1), `pageSize?` (defolt 20, maks 100) | `EssayHistoryResponse` | — |
| `GET /history/{id}` | — | `EssayDetailResponse` | `404` |
| `DELETE /history/{id}` | — | `204` | `404` |
| `DELETE /history` (hamısını sil) | — | `{deleted: number}` | — |

**`EssayDetailResponse`:**
```typescript
{
  id: number;
  title: string;
  createdAt: string;       // ISO datetime
  source: "Text" | "Image";
  wordCount: number;
  accuracyPercent: number; // 0-100
  totalScore: number;      // 0-5
  correctedEssay: string;  // <b>səhv</b> (düzəliş) formatında vurğulanmış HTML-vari mətn
  statistics: { grammar: number; spelling: number; vocabulary: number; naturalExpression: number; total: number };
  mistakes: Array<{ wrong: string; correct: string; category: "Grammar"|"Spelling"|"Vocabulary"|"NaturalExpression"; reason: string }>;
  scores: { structure: number; content: number; grammar: number; vocabulary: number; total: number }; // structure 0-1, content 0-2, grammar 0-1, vocabulary 0-1
  feedback: { strengths: string[]; weaknesses: string[]; recommendations: string[] };
}
```

**`EssayHistoryResponse`:**
```typescript
{
  items: Array<{ id: number; title: string; createdAt: string; wordCount: number; totalScore: number }>;
  totalCount: number;   // filtrlənmiş TAM say (cari səhifə deyil)
  averageScore: number; // filtrlənmiş tam dəst üzrə ortalama
  page: number;
  pageSize: number;
  totalPages: number;
}
```

### 4.4. Subscription (`/api/subscription`)

| Method + Path | Auth | Body | Cavab | Xəta |
|---|---|---|---|---|
| `GET /plans` | ✗ | — | `PlanInfoResponse[]` | — |
| `GET /` (cari abunəlik) | ✓ | — | `SubscriptionResponse` | — |
| `POST /subscribe` (**yalnız test/manual**, real Google Play üçün YOX) | ✓ | `{plan: "Pro"|"ProPlus", platform?, purchaseToken?, autoRenew?, durationDays?}` | `SubscriptionResponse` | `400` |
| `POST /cancel` | ✓ | — | `SubscriptionResponse` (Free-yə düşür) | — |
| `GET /usage` (gündəlik limit statusu) | ✓ | — | `DailyUsageStatusResponse` | — |
| `POST /google/verify` (**real Google Play axını**) | ✓ | `{productId, purchaseToken}` | `SubscriptionResponse` | `400` (bax `FRONTEND_GOOGLE_PLAY_BILLING.md`) |

**`PlanInfoResponse`:**
```typescript
{ plan: "Free"|"Pro"|"ProPlus"; name: string; price: number; currency: "AZN"; period: string;
  unlimitedText: boolean; dailyTextLimit: number | null; ocr: boolean; features: string[] }
```

**`SubscriptionResponse`:**
```typescript
{ plan: "Free"|"Pro"|"ProPlus"; isActive: boolean; startDate: string | null; endDate: string | null;
  autoRenew: boolean; platform: "Manual"|"GooglePlay"|"AppStore" | null }
```

**`DailyUsageStatusResponse`** (Əsas səhifədəki "Gündəlik pulsuz şans" bloku üçün):
```typescript
{ plan: "Free"|"Pro"|"ProPlus"; unlimitedText: boolean; dailyTextLimit: number | null;
  textUsedToday: number; textRemaining: number | null; canUseOcr: boolean; resetAtUtc: string }
```

### 4.5. Health

`GET /health` (auth yoxdur) → `200 "Healthy"` — istəsən app-in "backend əlçatandır" yoxlaması üçün istifadə et.

---

## 5. Biznes qaydaları (UI-da nəzərə alınmalı)

| Qayda | Detal |
|---|---|
| **Free plan** | Gündə 1 mətn yoxlaması, **OCR yoxdur** (`canUseOcr: false`) |
| **Pro plan** | Mətn limitsiz, **OCR yoxdur** |
| **Pro Plus plan** | Mətn + OCR limitsiz |
| **Gündəlik limit sıfırlanması** | **UTC gecə yarısı** — `resetAtUtc` sahəsini istifadə edib geri sayım göstər |
| **Esse mətn limiti** | Maks. 5000 simvol (`Əsas səhifə`-də "0 / 5000" göstəricisi buna uyğundur) |
| **Login lockout** | 5 yanlış cəhd → 15 dəqiqə bloklama (`423` + `lockoutEndsAt`) |
| **Access token ömrü** | ~120 dəqiqə — bundan sonra avtomatik `refresh` lazımdır |
| **Refresh token ömrü** | ~30 gün, rotasiya olunur (hər istifadədən sonra dəyişir) |
| **AI "bu esse deyil" desə** | `422` qaytarılır, istifadəçiyə "Bu mətn esse kimi tanınmadı" mesajı göstər, tarixçəyə yazılmır |

---

## 6. Tövsiyə olunan Frontend Tech Stack (React Native + TypeScript)

| Kateqoriya | Tövsiyə |
|---|---|
| **Naviqasiya** | React Navigation (native-stack + bottom-tabs) |
| **Server state / data fetching** | TanStack Query (React Query) — cache, retry, refetch-on-focus (planı/limiti təzələmək üçün əla) |
| **Client state** | Zustand (sadə) və ya Redux Toolkit (böyüsə) — auth token vəziyyəti, cari istifadəçi |
| **Formlar + validasiya** | `react-hook-form` + `zod` (backend-in DataAnnotation qaydalarına paralel sxemlər) |
| **Təhlükəsiz saxlama** | `react-native-keychain` (access/refresh token üçün) |
| **HTTP client** | `fetch` (built-in) və ya `axios` (interceptor ilə avtomatik `Authorization` header + 401-də refresh) |
| **Billing** | `react-native-iap` — ətraflı bax `FRONTEND_GOOGLE_PLAY_BILLING.md` |
| **UI kit** | Sərbəst seçim (NativeWind/Tailwind, React Native Paper, ya custom komponentlər) — dizayn artıq Figma-da hazırdır |

**Axios interceptor nümunəsi (konsept, kod deyil, məntiq):** hər sorğuya `Authorization: Bearer` əlavə et → cavab `401` olarsa, `refresh` çağır → uğurlu olarsa orijinal sorğunu təkrar et → uğursuz olarsa `logout` state-ə keç və Login ekranına yönləndir.

---

## 7. Nəzərə alınmalı kənar hallar

- **Refresh token race condition:** eyni anda bir neçə sorğu 401 alsa, `refresh`-i **yalnız bir dəfə** çağır (paralel refresh çağırışlarının qarşısını al — əks halda rotasiya səbəbindən ikinci refresh 401 alacaq).
- **Tarixçə pagination:** "Cəmi: N esse" göstəricisi `totalCount`-dan gəlir (cari səhifədəki element sayından deyil).
- **OCR axını:** şəkil → `POST /essay/ocr` → qaytarılan mətni **redaktə edilə bilən input-a** yerləşdir → istifadəçi düzəliş edib **ayrıca** `POST /essay/evaluate`-ə (source: "Image") göndərir. Bu iki addım bir-birindən asılı deyil.
- **Planlar ekranı qiymətləri:** `GET /plans`-dan gələn `price`/`features` istifadə et (Google-un `localizedPrice`-ını yalnız ödəniş pəncərəsində əlavə göstərici kimi istifadə et — bax billing sənədi).

---

## 8. Hələ qərar verilməmiş / gözləyən

- Google/Apple sosial giriş — backend-də yoxdur, UI-da düymə göstərilə bilər amma "tezliklə" statusunda saxla.
- iOS/App Store billing — backend dəstəkləmir, hazırda yalnız Android/Google Play.
- Dil (i18n) və Bildirişlər (push notifications) ekranları — məhsul qərarı ilə Ayarlar-dan çıxarılıb, backend-də yoxdur.
