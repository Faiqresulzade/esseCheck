# EssayCheck AI — Planların Məntiqi Dəyişdi (Unified Limit)

Bu sənəd `/api/Subscription/plans` və `/api/Subscription/usage` cavablarının formasında olan **breaking dəyişikliyi** izah edir. Köhnə sahələr silinib, yeniləri fərqli semantikaya malikdir.

---

## 1. Nə dəyişdi (məntiq)

Əvvəl mətnlə yoxlama və şəkillə (OCR) yoxlama tamamilə ayrı iki qayda ilə idarə olunurdu:
- Mətn: Free gündə 1, Pro/Pro Plus limitsiz
- OCR: yalnız Pro Plus istifadə edə bilirdi (heç Free, heç Pro yox), amma limitsiz idi

**İndi hər ikisi eyni gündəlik say ğaca sayılır** — mətnlə yoxlama ilə şəkildən yoxlama arasında fərq yoxdur, ikisi də eyni limiti azaldır:

| Plan | Gündəlik limit (mətn + şəkil birlikdə) |
|---|---|
| Free | **1** |
| Pro | **10** |
| Pro Plus | **Limitsiz** |

Yəni Free istifadəçi gündə 1 dəfə **istənilən növdən** (mətnlə və ya şəkillə) yoxlama edə bilər — əvvəlki kimi "Free/Pro şəkil göndərə bilməz" məhdudiyyəti **artıq yoxdur**.

---

## 2. `GET /api/Subscription/plans` — yeni cavab forması

```json
[
  {
    "plan": "Free",
    "name": "Free",
    "price": 0,
    "currency": "USD",
    "period": "ay",
    "unlimited": false,
    "dailyLimit": 1,
    "features": ["Gündə 1 esse şansı (mətnlə və ya şəkillə)", "Tarixçə (pulsuz)"]
  },
  {
    "plan": "Pro",
    "name": "Pro",
    "price": 2.99,
    "currency": "USD",
    "period": "ay",
    "unlimited": false,
    "dailyLimit": 10,
    "features": ["Gündə 10 esse şansı (mətnlə və ya şəkillə)", "Tarixçə (pulsuz)"]
  },
  {
    "plan": "ProPlus",
    "name": "Pro Plus",
    "price": 5.99,
    "currency": "USD",
    "period": "ay",
    "unlimited": true,
    "dailyLimit": null,
    "features": ["Limitsiz esse (mətnlə və ya şəkillə)", "Tarixçə (pulsuz)"]
  }
]
```

**Silinən sahələr:** `unlimitedText`, `dailyTextLimit`, `ocr` — bunları oxuyan kod indi `undefined` alacaq.
**Yeni sahələr:** `unlimited` (bool), `dailyLimit` (number | null) — mətn/şəkil ayrımı olmadan, ümumi limit.

**Qiymət diqqəti:** `currency` indi `"AZN"` yox, `"USD"`-dir, dəyərlər də dəyişib (Pro: 2.99, Pro Plus: 5.99). UI-da valyuta simvolunu (`$`) buna görə yeniləyin.

---

## 3. `GET /api/Subscription/usage` — yeni cavab forması

```json
{
  "plan": "Free",
  "unlimited": false,
  "dailyLimit": 1,
  "usedToday": 0,
  "remaining": 1,
  "resetAtUtc": "2026-08-17T00:00:00Z"
}
```

**Silinən sahələr:** `unlimitedText`, `dailyTextLimit`, `textUsedToday`, `textRemaining`, `canUseOcr`.
**Yeni sahələr:** `unlimited`, `dailyLimit`, `usedToday`, `remaining` — indi bunlar mətn/şəkil ayrımı olmadan **ümumi** göstəricilərdir.

Əsas səhifədəki "Gündəlik pulsuz şans" bloku bu sahələrə görə yenidən adlandırılmalıdır (məs. "Bugün 1/1 istifadə etdiniz" — həm mətn, həm şəkil bura daxildir).

---

## 4. `canUseOcr` sahəsi tamamilə silinib — UI davranışı

Əvvəl frontend `canUseOcr` sahəsinə baxıb şəkil yükləmə düyməsini gizlədə/göstərə bilərdi (yalnız Pro Plus-da göstərilirdi). **Bu sahə artıq yoxdur, çünki bütün planlar OCR-dan istifadə edə bilər** (sadəcə gündəlik limitə sayılır).

Tövsiyə: şəkil yükləmə düyməsini **bütün planlarda göstərin**. İstifadəçi limitini bitiribsə, backend özü 429 statusu ilə "Bugünkü limit (N) bitib..." mesajını qaytaracaq — bunu error kimi göstərin (bax aşağı).

---

## 5. OCR rədd statusu dəyişdi: 403 → 429

Əvvəl OCR-a icazə yoxdursa (plan qadağası) `403 Forbidden` qaytarılırdı. İndi bu, sadəcə gündəlik limit məsələsi olduğu üçün **mətn yoxlaması ilə eyni status kodu — `429 Too Many Requests`** qaytarılır:

```json
{ "message": "Bugünkü limit (1) bitib. Sabah yenilənəcək və ya planınızı yüksəldin." }
```

Frontend-də əgər `403` statusuna görə xüsusi "Pro Plus lazımdır" mesajı göstərən kod varsa, onu silin — indi bütün limit-aşımı halları (həm mətn, həm OCR) `429` ilə gəlir və eyni ümumi mesajı göstərməlidir.

---

## 6. Yekun yoxlama siyahısı (frontend üçün)

- [ ] `/api/Subscription/plans` cavabında `unlimited`/`dailyLimit` oxunur, köhnə `unlimitedText`/`dailyTextLimit`/`ocr` sahələrinə istinad qalmayıb
- [ ] `/api/Subscription/usage` cavabında `usedToday`/`remaining` oxunur, köhnə `textUsedToday`/`textRemaining`/`canUseOcr` sahələrinə istinad qalmayıb
- [ ] Qiymətlər USD (`$2.99` / `$5.99`) kimi göstərilir, AZN yox
- [ ] Şəkil yükləmə düyməsi bütün planlarda görünür (Pro Plus-a məxsus deyil)
- [ ] `429` statuslu cavab (həm `/evaluate`, həm `/ocr`, həm `/evaluate/grade9-images` üçün) eyni şəkildə "limit bitib" mesajı kimi göstərilir
- [ ] "Gündəlik pulsuz şans" bloku mətn+şəkili birlikdə göstərir (ayrı-ayrı deyil)
