# EssayCheck AI — Sinif Səviyyəsi (Grade9 / Grade11) Dəyişikliyi

Bu sənəd yalnız esse qiymətləndirmə axınına edilən **bir dəyişikliyi** əhatə edir: DİM qiymətləndirmə meyarları 9-cu və 11-ci siniflər üçün fərqli olduğundan, backend indi **hansı sinif üçün qiymətləndirildiyini** bilməlidir. Bunun üçün frontend `POST /api/Essay/evaluate` sorğusuna **yeni, məcburi bir sahə** əlavə etməlidir: `grade`.

Backend tərəfi tamamilə hazırdır və test edilib. Bu sənəd frontend-in nəyi dəyişməli olduğunu göstərir.

---

## 1. Nə üçün bu dəyişiklik lazımdır

- 9-cu sinif üçün minimum söz sayı **35**, 11-ci sinif üçün **100**-dür. Bundan az yazılsa, bal avtomatik aşağı düşür.
- Hər iki sinifin bal sistemi eynidir (Structure 0-1, Content 0-2, Grammar 0-1, Vocabulary 0-1, cəmi maks. **5**) — yalnız söz sayı tələbi fərqlidir.
- Backend AI-a hansı promptu göndərəcəyini bu sahəyə görə seçir, ona görə **sahə göndərilmədən sorğu qəbul olunmur**.

---

## 2. Frontend-də UI dəyişikliyi

Esse yazma/göndərmə ekranında istifadəçi **əvvəlcədən sinifini seçməlidir** (9-cu sinif / 11-ci sinif) — bu, ya:
- ekranın yuxarısında sabit bir seçici (toggle/segmented control), ya da
- istifadəçi profilində bir dəfə seçilib yadda saxlanılan (və istənilən vaxt dəyişdirilə bilən) parametr

kimi göstərilə bilər. Hansı UI yanaşmasını seçəcəyiniz sizin qərarınızdır — backend üçün önəmli olan yalnız hər `/api/Essay/evaluate` sorğusunda düzgün dəyərin göndərilməsidir.

**OCR axınına təsiri yoxdur:** `POST /api/Essay/ocr` (şəkildən mətn oxuma) heç dəyişmədi, `grade` tələb etmir. Sinif seçimi yalnız son addımda, `POST /api/Essay/evaluate` çağırışında lazımdır (OCR-dan gələn mətn də daxil olmaqla).

---

## 3. Yeni sahə: `grade`

**Enum dəyərləri (dəqiq bu yazılışla, böyük hərflə):**

| Dəyər | Mənası |
|---|---|
| `"Grade9"` | 9-cu sinif (minimum 35 söz) |
| `"Grade11"` | 11-ci sinif (minimum 100 söz) |

> ⚠️ **Böyük/kiçik hərfə həssasdır.** `"grade9"`, `"GRADE9"`, `"grade_9"` kimi yazılışlar **qəbul olunmur** — dəqiq `"Grade9"` / `"Grade11"` olmalıdır.

---

## 4. Yenilənmiş sorğu — `POST /api/Essay/evaluate`

**Əvvəl:**
```json
{
  "text": "Nowadays, technology plays an important role...",
  "title": "Texnologiya haqqında",
  "source": "Text"
}
```

**İndi (yeni `grade` sahəsi əlavə olunub):**
```json
{
  "text": "Nowadays, technology plays an important role...",
  "title": "Texnologiya haqqında",
  "source": "Text",
  "grade": "Grade9"
}
```

| Sahə | Tip | Məcburidirmi | Qeyd |
|---|---|---|---|
| `text` | string | Bəli | Dəyişməyib (maks. 5000 simvol) |
| `title` | string? | Xeyr | Dəyişməyib |
| `source` | `"Text"` \| `"Image"` | Xeyr (defolt `"Text"`) | Dəyişməyib |
| **`grade`** | `"Grade9"` \| `"Grade11"` | **Bəli — yeni** | Göndərilməsə sorğu 400 xətası ilə rədd olunur |

---

## 5. Xəta halları (yeni)

**`grade` sahəsi ümumiyyətlə göndərilməyibsə** → HTTP 400:
```json
{
  "succeeded": false,
  "message": "Sinif dəyəri etibarsızdır.",
  "errors": ["Sinif dəyəri etibarsızdır."]
}
```

**`grade` səhv/naməlum dəyərlə göndərilibsə** (məs. `"Grade10"`, `"9"`, `"grade9"`) → HTTP 400:
```json
{
  "succeeded": false,
  "message": "The request field is required.",
  "errors": [
    "The request field is required.",
    "The JSON value could not be converted to EssayChecker.Domain.Enums.GradeLevel. Path: $.grade | ..."
  ]
}
```
(Bu ikinci mesaj JSON-un özünün deserializasiya xətasıdır, ona görə ingiliscədir — amma HTTP 400 statusu və `errors` massivi eyni formadadır, xüsusi işləmə tələb etmir.)

**Tövsiyə:** Frontend-də `grade`-i göndərməzdən əvvəl mütləq təyin edilmiş olduğunu yoxlayın (məs. default state-i `null` yox, `"Grade9"` və ya `"Grade11"` seçin ki, istifadəçi unutsa belə sorğu 400 almasın).

---

## 6. Cavab formatına (response) əlavə olunan sahə

Qiymətləndirmə nəticəsində (`evaluate` cavabı) və tarixçə endpoint-lərində (`GET /api/Essay/history`, `GET /api/Essay/history/{id}`) indi `grade` sahəsi də qaytarılır:

**`POST /api/Essay/evaluate` cavabı (200):**
```json
{
  "id": 42,
  "title": "Texnologiya haqqında",
  "createdAt": "2026-08-04T15:18:17Z",
  "source": "Text",
  "grade": "Grade9",
  "wordCount": 46,
  "accuracyPercent": 60,
  "totalScore": 3,
  "correctedEssay": "...",
  "statistics": { "...": "..." },
  "mistakes": [ "..." ],
  "scores": { "structure": 0.5, "content": 0.5, "grammar": 1, "vocabulary": 1, "total": 3 },
  "feedback": { "...": "..." }
}
```

**`GET /api/Essay/history` siyahı elementi:**
```json
{
  "id": 42,
  "title": "Texnologiya haqqında",
  "createdAt": "2026-08-04T15:18:17Z",
  "wordCount": 46,
  "totalScore": 3,
  "grade": "Grade9"
}
```

**Tövsiyə:** Tarixçə ekranında hər qeydin yanında hansı sinif üçün qiymətləndirildiyini (məs. kiçik bir "9-cu sinif" / "11-ci sinif" etiketi) göstərmək faydalı olar, çünki istifadəçi vaxtaşırı fərqli sinif seçə bilər.

> **Köhnə tarixçə qeydləri** (bu dəyişiklikdən əvvəl yaradılmış essələr) backend tərəfindən avtomatik `"Grade11"` kimi işarələnib — bu, real dəyər deyil, sadəcə köhnə məlumatın boş qalmaması üçün defolt qiymətdir.

---

## 7. Yekun yoxlama siyahısı (frontend üçün)

- [ ] Esse göndərmə ekranına sinif seçici (9-cu / 11-ci) əlavə edildi
- [ ] `POST /api/Essay/evaluate` sorğusuna `grade: "Grade9"` və ya `"Grade11"` əlavə olundu
- [ ] `grade` seçilmədən "Yoxla" düyməsi basılmasın deyə frontend-side validasiya əlavə olundu (backend-ə boş getməsin)
- [ ] Tarixçə siyahısı və detal ekranında yeni `grade` sahəsi UI-da göstərilir (istəyə bağlı, amma tövsiyə olunur)
