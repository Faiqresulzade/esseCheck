# EssayCheck AI — 4-cü Plan (Premium) və Yenilənmiş Limitlər

Bu sənəd 2026-08-23 tarixli **breaking dəyişikliyi** izah edir. `FRONTEND_UNIFIED_PLAN_LIMITS.md`
köhnəlib — orada `ProPlus` "limitsiz" göstərilir və cəmi 3 plan var idi. Bu sənəd həmin faylın
yerini tutur, cari (canlı, backend-də test olunmuş) vəziyyəti əks etdirir.

---

## 1. Nə dəyişdi

**Yeni plan əlavə olundu: `Premium`.** İndi 4 plan var: `Free`, `Pro`, `ProPlus`, `Premium`.

**`ProPlus` artıq limitsiz deyil.** Əvvəl `Pro Plus` planı həqiqətən limitsiz esse yoxlaması verirdi
(`unlimited: true, dailyLimit: null`). İndi **20/gün** ədədi həddə düşüb. Əgər UI-da "Pro Plus =
limitsiz" mətni varsa, dəyişdirin.

**Esse limitləri:**

| Plan | Esse/gün | Qiymət |
|---|---|---|
| Free | 1 | 0 (pulsuz) |
| Pro | 10 | $2.99/ay |
| Pro Plus | 20 | $5.99/ay |
| **Premium** | **40** | **$10.99/ay** |

**Dərs (mövzu izahı) limitləri artıq pillələnib** — əvvəl bütün planlarda sabit 1/gün idi, indi fərqlidir:

| Plan | Dərs/gün |
|---|---|
| Free | 1 |
| Pro | 1 |
| Pro Plus | 2 |
| Premium | 4 |

> Qeyd: dərs limiti yalnız **yeni mövzu yaratmağa** aiddir. Kitabxanadakı istənilən dərsi (kim
> yaradıbsa fərqi yoxdur) bütün planlar **limitsiz** oxuyur — bax `FRONTEND_LESSONS.md`.

---

## 2. `GET /api/subscription/plans` — yeni cavab

```json
[
  {
    "plan": "Free", "name": "Free", "price": 0, "currency": "USD", "period": "ay",
    "unlimited": false, "dailyLimit": 1,
    "features": ["Gündə 1 esse yoxlama", "Limitsiz tarixçə", "Gündə 1 AI dərs izahı", "Şagird analitikası"]
  },
  {
    "plan": "Pro", "name": "Pro", "price": 2.99, "currency": "USD", "period": "ay",
    "unlimited": false, "dailyLimit": 10,
    "features": ["Gündə 10 esse yoxlama", "Limitsiz tarixçə", "Gündə 1 AI dərs izahı", "Şagird analitikası"]
  },
  {
    "plan": "ProPlus", "name": "Pro Plus", "price": 5.99, "currency": "USD", "period": "ay",
    "unlimited": false, "dailyLimit": 20,
    "features": ["Gündə 20 esse yoxlama", "Limitsiz tarixçə", "Gündə 2 AI dərs izahı", "Şagird analitikası"]
  },
  {
    "plan": "Premium", "name": "Premium", "price": 10.99, "currency": "USD", "period": "ay",
    "unlimited": false, "dailyLimit": 40,
    "features": ["Gündə 40 esse yoxlama", "Limitsiz tarixçə", "Gündə 4 AI dərs izahı", "Şagird analitikası"]
  }
]
```

Bu cavab canlı sistemdə yoxlanılıb, uydurma deyil.

**Diqqət — `unlimited`/`dailyLimit` real dəyərlərdir, marketinq mətni deyil.** Premium-u mağazada
"limitsiz esse yoxlama" kimi tanıda bilərsiniz (bu, sizin marketinq qərarınızdır), **amma API
Premium üçün belə `unlimited: false, dailyLimit: 40` qaytarır** — çünki arxa planda real fair-use
həddi var (real AI xərcinə görə hesablanıb). Yəni:

- Paywall/marketinq ekranında istədiyiniz mətni yaza bilərsiniz ("Limitsiz!"),
- **Amma** `/usage` ekranında (aşağı bax) real ədədi ("32/40 qaldı") göstərməyi tövsiyə edirik —
  əks halda istifadəçi 41-ci sorğuda gözlənilməz `429` alacaq və bu, mağazadakı vədlə ziddiyyət
  təşkil edəcək.

---

## 3. `GET /api/subscription/usage` — yeni cavab

```json
{
  "plan": "Premium",
  "unlimited": false,
  "dailyLimit": 40,
  "usedToday": 0,
  "remaining": 40,
  "resetAtUtc": "2026-08-24T00:00:00Z",

  "lessonUnlimited": false,
  "lessonDailyLimit": 4,
  "lessonsUsedToday": 0,
  "lessonRemaining": 4
}
```

Bu, canlı sistemdə test hesabı `Premium`-a keçirilərək əldə edilmiş **real cavabdır**. `lesson*`
sahələri `FRONTEND_LESSONS.md`-də izah olunan dərs limitidir — esse sahələrindən **tam ayrıdır**,
iki sayğacı ekranda ayrı göstərin.

---

## 4. Google Play — məhsul ID-ləri

Play Console-da 3 pullu abunəlik məhsulu var (siz təsdiqlədiniz):

| Play Console məhsul ID-si | Daxili plan |
|---|---|
| `pro_monthly` | `Pro` |
| `pro_plus_monthly` | `ProPlus` |
| `premium` | `Premium` |

`POST /api/subscription/google/verify` çağırışında `productId` sahəsinə **məhz bu ID-ləri**
göndərin (Play Billing kitabxanasının `SkuDetails`/`ProductDetails`-dən aldığı ID ilə eynidir).

⚠️ **Diqqət:** Play Console-da "Premium" məhsulunun **görünən adı** `premium_monthly`-dir, amma
**ID-si** `premium`-dur (digər iki məhsulda ad və ID nümunəsi əksinədir: "Pro" adı, `pro_monthly`
ID-si). Play Billing kitabxanası real ID-ni (`premium`) qaytaracaq, ona görə client tərəfdə əlavə
bir şey etməyə ehtiyac yoxdur — sadəcə naming-in qeyri-simmetrik olduğunu bilin, kodda sabit sətir
yazırsınızsa səhv etməyin.

---

## 5. Xəta halları (dəyişməyib, xatırlatma)

`429` — gündəlik limit bitib:
```json
{ "message": "Bugünkü limit (40) bitib. Sabah yenilənəcək və ya planınızı yüksəldin." }
```

Bu, bütün planlarda (Premium daxil) eyni formatdadır — `403` yoxdur, plan-spesifik xüsusi mesaj yoxdur.

---

## 6. Yekun yoxlama siyahısı (frontend üçün)

- [ ] Paywall/planlar ekranında **4 plan** göstərilir (Free/Pro/Pro Plus/Premium)
- [ ] "Pro Plus = limitsiz" mətni silinib, "Gündə 20 esse" ilə əvəzlənib
- [ ] Premium marketinq mətni istənilən ("limitsiz") ola bilər, amma `/usage` ekranında real
      `40` ədədi göstərilir ki, istifadəçi limitə çatanda çaşmasın
- [ ] `/usage`-da iki ayrı sayğac: esse (`dailyLimit`/`remaining`) və dərs (`lessonDailyLimit`/`lessonRemaining`)
- [ ] Google Play satınalmasında `productId` üçün `pro_monthly` / `pro_plus_monthly` / `premium`
      dəqiq göndərilir (Billing kitabxanasından gələn dəyər, əl ilə yazılmır)
- [ ] Qiymətlər: Pro $2.99, Pro Plus $5.99, Premium $10.99 (USD, `$` işarəsi)
