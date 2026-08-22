# EssayCheck AI — Mövzu İzahı (Dərs) Funksiyası — Frontend Sənədi

Bu sənəd `BACKEND_LESSON_FEATURE.md` sifarişinə cavabdır: **backend hazırdır və canlı test olunub**.
Aşağıda kontraktdakı fərqlər, real cavab nümunələri və diqqət etməli olduğunuz məqamlar var.

**Bu, breaking dəyişiklik deyil** — mövcud endpoint-lərin heç birinə toxunulmayıb.
`/api/subscription/usage` cavabına yalnız **yeni sahələr əlavə olunub**, köhnələri olduğu kimi qalıb.

---

## 1. Sifarişdən fərqlər (qısa)

Kontraktın demək olar hamısı olduğu kimi qurulub. Üç yerdə fərq var:

| Nə | Sifarişdə | Reallıqda | Səbəb |
|---|---|---|---|
| Eyni mövzunu təkrar soruşmaq | Aydın deyildi | **Mövcud dərs qaytarılır, limit xərclənmir** | Dublikat yaranmasın, limit boşuna getməsin |
| `GET /api/lessons` filtrləri | `search`, `studentId` | + **`groupId`** əlavə olundu | Esse tarixçəsi ilə eyni olsun |
| Test variantlarının sırası | — | **Backend variantları qarışdırır** (§4.1) | AI hər dəfə düzgün cavabı 1-ci variantda verirdi |

---

## 2. Endpoint-lər

Hamısı `[Authorize]` — mövcud `Authorization: Bearer {accessToken}` başlığı kifayətdir.

| Metod | Yol | Limit xərcləyir? |
|---|---|---|
| `POST` | `/api/lessons` | ✅ Bəli (şərtlə — §5) |
| `GET` | `/api/lessons` | ❌ Yox |
| `GET` | `/api/lessons/{id}` | ❌ Yox |
| `DELETE` | `/api/lessons/{id}` | ❌ Yox |

Başqasının dərsinə müraciət → **`404 { "message": "Dərs tapılmadı." }`** (`403` yox — mövcudluq faktı sızmasın).

### 2.1. `POST /api/lessons`

```json
{ "topic": "Present Perfect", "grade": "Grade11", "studentId": 4 }
```

- `topic` — məcburi, maksimum 200 simvol.
- `grade` — **opsional**. Göndərilməyibsə və `studentId` verilibsə şagirdin kartındakı sinif işlədilir.
  İkisi də yoxdursa → `400 { "message": "Sinif seçilməlidir." }`
- `studentId` — **opsional**, yalnız etiket/filtr üçündür. Mövcud deyilsə/başqasınındırsa →
  `400 { "message": "Şagird tapılmadı." }` və **AI çağırılmır, limit toxunulmur**.

Cavab: `200` + §3-dəki `LessonResponse`.

### 2.2. `GET /api/lessons`

Query: `search`, `studentId`, `groupId`, `page` (default 1), `pageSize` (default 20, maks 100).

```json
{
  "items": [
    { "id": 2, "topic": "Present Perfect", "grade": "Grade9", "studentId": null,
      "studentName": null, "slideCount": 7, "createdAt": "2026-08-22T10:49:46.09Z" }
  ],
  "totalCount": 2, "page": 1, "pageSize": 20, "totalPages": 1
}
```

Slaydların məzmunu **qaytarılmır** — yalnız `slideCount`. Yeni dərslər əvvəldədir.

---

## 3. `LessonResponse` — real cavab

Aşağıdakı nümunə **canlı sistemdən götürülüb** (uydurma deyil), yalnız qısaldılıb:

```json
{
  "id": 1,
  "topic": "Present Perfect",
  "grade": "Grade11",
  "studentId": null,
  "studentName": null,
  "createdAt": "2026-08-22T10:48:44.85Z",
  "slides": [
    {
      "type": "Intro",
      "title": "Present Perfect Nədir?",
      "body": "Present Perfect zamanını öyrənmək, ingilis dilindəki yazılarınızda daha dəqiq ifadələr yaratmağa kömək edəcək...",
      "formula": null, "keywords": [], "examples": [], "mistakes": [], "comparison": null, "points": []
    },
    {
      "type": "Rule",
      "title": "Present Perfect Qaydasını Öyrən",
      "body": "Present Perfect zamanını formalaşdırmaq üçün \"have/has + V3\" formasını istifadə edirik...",
      "formula": "have / has + V3",
      "keywords": ["have", "has", "done", "seen", "ever", "never", "just", "yet"],
      "examples": [], "mistakes": [], "comparison": null, "points": []
    },
    {
      "type": "Examples",
      "title": "Nümunələr",
      "body": null, "formula": null, "keywords": [],
      "examples": [
        { "en": "I have visited London.", "az": "Mən Londonu ziyarət etmişəm.", "highlight": "have visited" },
        { "en": "She has never eaten sushi.", "az": "O, heç vaxt suşi yeməyib.", "highlight": "has never eaten" }
      ],
      "mistakes": [], "comparison": null, "points": []
    },
    {
      "type": "Mistakes",
      "title": "Səhvlər",
      "body": "Azərbaycanlı tələbələr tez-tez bu zamanı düzgün istifadə etmirlər.",
      "formula": null, "keywords": [], "examples": [],
      "mistakes": [
        { "wrong": "I seen that movie.", "correct": "I have seen that movie.",
          "note": "V3 formasını istifadə etməyi unutmayın." }
      ],
      "comparison": null, "points": []
    },
    {
      "type": "Compare",
      "title": "Present Perfect vs. Simple Past",
      "body": "Bu iki zaman arasında fərq var...",
      "formula": null, "keywords": [], "examples": [], "mistakes": [],
      "comparison": {
        "leftTitle": "Present Perfect",
        "leftBody": "Keçmişdə baş vermiş, lakin indiki zamana təsiri olan hadisələr. Məsələn: \"I have finished my work.\"",
        "rightTitle": "Simple Past",
        "rightBody": "Keçmişdə baş vermiş, indiki zamana təsiri olmayan hadisələr. Məsələn: \"I finished my work yesterday.\""
      },
      "points": []
    },
    {
      "type": "Summary",
      "title": "Xülasə",
      "body": "Unutmayın ki, bu zaman \"have/has + V3\" formasını istifadə edir...",
      "formula": null, "keywords": [], "examples": [], "mistakes": [], "comparison": null,
      "points": [
        "Present Perfect zamanını öyrənmək vacibdir.",
        "\"have/has + V3\" formasını istifadə edin.",
        "Keçmiş hadisələrin indiki zamana təsirini vurğulayın."
      ]
    }
  ],
  "quiz": [
    {
      "question": "The book ___ by the author.",
      "options": ["are written", "were written", "wrote", "is written"],
      "correctIndex": 3,
      "explanation": "Passive Voice-da \"is written\" düzgün formadır, çünki mübtəda tək haldadır."
    }
  ]
}
```

### 3.1. Zəmanətlər (backend təmin edir)

✅ **Bütün 9 sahə hər slaydda var** — istifadə olunmayanlar `null` və ya boş massivdir.
Frontend-də "sahə mövcuddurmu?" yoxlaması lazım deyil, birbaşa `slide.examples.length` yaza bilərsiniz.
(10 slayd üzərində yoxlandı: 9/9 sahə hər slaydda.)

✅ **`type` yalnız bu 6 dəyərdən biridir:** `Intro`, `Rule`, `Examples`, `Mistakes`, `Compare`, `Summary`.

✅ **`title` heç vaxt `null` deyil.**

✅ **`quiz[].correctIndex` həmişə `0 ≤ correctIndex < options.length`** — kənarda olan sual backend-də atılır.
Yəni `options[correctIndex]` təhlükəsizdir, əlavə yoxlama lazım deyil.

### 3.2. Zəmanət OLMAYAN (AI-dan asılı)

⚠️ **Slayd sayı** — hədəf 6-8-dir, amma **zəmanət deyil**. Testdə həmişə 7 gəldi, lakin 5 və ya 9 da gələ bilər.
Ekranı slayd sayına uyğunlaşdırın, sabit 7 gözləməyin. Ardıcıllıq praktikada həmişə düzgün gəldi
(`Intro → Rule → Examples → Examples → Mistakes → Compare → Summary`), amma buna da bel bağlamayın —
slaydları **gələn sıra ilə** göstərin, `type`-a görə yenidən sıralamayın.

⚠️ **Test sualı sayı** — hədəf 3-dür. Sınıq sual atıldığı üçün 2 də ola bilər. `quiz.length`-ə baxın.

⚠️ **`examples[].highlight`** — `en` cümləsinin hərfi alt-sətri olmalıdır və testdə **4/4** belə oldu,
amma zəmanət deyil. Tapılmasa sadəcə vurğulamayın, xəta göstərməyin.

⚠️ **`Examples` slaydı 1 və ya 2 dəfə gələ bilər** (testdə 2 dəfə gəldi). Bu normaldır.

---

## 4. Mini test

- `options` — adətən 4 variant.
- `correctIndex` — 0-dan başlayan indeks, həmişə etibarlıdır (§3.1).
- `explanation` — cavab verildikdən sonra göstərilir.

### 4.1. Variantları YENİDƏN QARIŞDIRMAYIN

Backend variantları artıq qarışdırır. Səbəb: AI hər dəfə düzgün cavabı **1-ci variantda** verirdi
(ölçmə: iki dərsdə 6 sualın 6-sı da `correctIndex: 0`). Şagird bunu bir neçə dərsdən sonra öyrənərdi.

İndi hər dərsdə cavablar müxtəlif mövqelərdədir — real nəticələr: `[2,3,0]`, `[0,1,2]`, `[3,0,1]`.

Qarışdırma **deterministikdir**: eyni dərs hər açılışda eyni sıranı verir. Ona görə:

- **Frontend tərəfdə əlavə `shuffle()` etməyin** — hər açılışda sıra dəyişsə, PDF-də və ekranda
  fərqli sıra çıxar, şagird isə eyni dərsi ikinci dəfə açanda başqa mənzərə görər.
- Variantları gəldiyi sıra ilə göstərin.

---

## 5. Limit — esse limitindən TAM AYRI

| Plan | Gündəlik dərs |
|---|---|
| Free | 1 |
| Pro | 5 |
| ProPlus | Limitsiz |

Esse limiti ilə **heç bir əlaqəsi yoxdur**: Free istifadəçi bir gündə həm 1 esse, həm 1 dərs ala bilər.
Sıfırlanma vaxtı eynidir (`resetAtUtc`).

### 5.1. Limit nə vaxt xərclənir

| Hal | Limit | AI çağırılır? |
|---|---|---|
| Yeni mövzu | ✅ xərclənir | ✅ bəli |
| Başqasının əvvəl soruşduğu mövzu (keş) | ✅ xərclənir | ❌ yox (~2 san) |
| **Sizin siyahınızda artıq olan mövzu** | ❌ **xərclənmir** | ❌ yox (dərhal) |
| Mövzu İngilis dilinə aid deyil (`422`) | ❌ xərclənmir | — |
| Şagird tapılmadı (`400`) | ❌ xərclənmir | ❌ yox |

> **Vacib:** istifadəçi eyni mövzunu təkrar yazsa, yeni dərs yaranmır — **mövcud dərs qaytarılır**
> (eyni `id` ilə). "Yenidən yarat" düyməsi hazırda **yoxdur**; lazım olsa deyin, əlavə edərik.
> Normalizasiya: `"present   PERFECT"` və `"Present Perfect"` eyni mövzu sayılır.

### 5.2. `/api/subscription/usage` — real cavab

```json
{
  "plan": "Free",
  "unlimited": false,
  "dailyLimit": 1,
  "usedToday": 0,
  "remaining": 1,
  "resetAtUtc": "2026-08-23T00:00:00Z",

  "lessonUnlimited": false,
  "lessonDailyLimit": 1,
  "lessonsUsedToday": 1,
  "lessonRemaining": 0
}
```

Yuxarıdakı real cavabda görünür: dərs limiti bitib (`lessonRemaining: 0`), esse limiti isə
toxunulmayıb (`remaining: 1`). İki sayğacı ekranda **ayrı-ayrı** göstərin.

`lessonUnlimited: true` olduqda `lessonDailyLimit` və `lessonRemaining` **`null`** gəlir
(esse sayğacındakı eyni məntiq) — `null` halını idarə edin.

---

## 6. Xəta halları

| Status | Nə vaxt | Cavab |
|---|---|---|
| `400` | Sinif yoxdur | `{ "message": "Sinif seçilməlidir." }` |
| `400` | Şagird tapılmadı | `{ "message": "Şagird tapılmadı." }` |
| `422` | Mövzu İngilis dilinə aid deyil | `{ "message": "Bu mövzu İngilis dili dərsinə aid deyil. İngilis dili ilə bağlı mövzu yazın." }` |
| `429` | Gündəlik dərs limiti bitib | `{ "message": "Bugünkü dərs limitiniz (1) bitib. Sabah yenilənəcək və ya planınızı yüksəldin." }` |
| `404` | Dərs yoxdur / başqasınındır | `{ "message": "Dərs tapılmadı." }` |
| `503` / `502` | AI əlçatmazdır | mövcud esse axınındakı mesaj |

`422` mövzu yoxlaması üçün **ayrıca sorğu yoxdur** — bir çağırışda həll olunur, yəni "yoxla, sonra yarat"
kimi iki addımlı axın qurmağa ehtiyac yoxdur.

---

## 7. Gözlənilən sürət

Real ölçmələr (`gpt-4o-mini`):

| Hal | Vaxt |
|---|---|
| Yeni mövzu (AI) | **12-17 saniyə** |
| Keşdən (başqası əvvəl soruşub) | **~2 saniyə** |
| Öz siyahınızdakı mövzu | **<1 saniyə** |

12-17 saniyə uzundur — yükləmə ekranını buna görə qurun (skeleton slayd, "dərs hazırlanır..." animasiyası).
İstifadəçi eyni mövzunu ikinci dəfə yazsa dərhal açılacaq, bu fərqi izah etməyə ehtiyac yoxdur.

---

## 8. Məzmun keyfiyyəti barədə dürüst qeyd

Hazırda `gpt-4o-mini` işlədilir (ucuz model). Ölçmə nəticəsi:

- ✅ **Struktur etibarlıdır** — slaydlar, sahələr, `highlight`, test formatı testdə problemsiz gəldi.
- ⚠️ **`Grade9` və `Grade11` fərqi zəifdir** — izah uzunluğu 703 vs 975 simvol, nümunələr demək olar eyni
  (`I have visited Paris` / `I have visited London`). Sinif seçimini UI-da göstərin, amma
  "9-cu sinif üçün xüsusi hazırlanıb" kimi güclü vəd verməyin.
- ⚠️ **Xülasə bəndləri bəzən ümumidir** ("Present Perfect zamanını öyrənmək vacibdir").

Model dəyişdirilməsi backend tərəfdə **bir sətir konfiqurasiyadır** — keyfiyyət problem olarsa deyin.

---

## 9. Yekun yoxlama siyahısı (frontend üçün)

- [ ] Mövzu yazma ekranı + `POST /api/lessons` (12-17 saniyəlik yükləmə vəziyyəti)
- [ ] Slaydlar **gələn sıra ilə** göstərilir, sayı sabit qəbul edilmir
- [ ] Hər `type` üçün ayrıca görünüş (`Rule`-da `formula`+`keywords`, `Compare`-də iki sütun və s.)
- [ ] `examples[].en` TTS ilə oxunur; `highlight` tapılmasa sadəcə vurğulanmır
- [ ] Test: `options[correctIndex]` birbaşa işlədilir, **əlavə qarışdırma yoxdur**
- [ ] `422` (uyğunsuz mövzu) və `429` (limit) mesajları göstərilir
- [ ] Təkrar mövzu → eyni dərs açılır (yeni yaranmır) — istifadəçini çaşdırmayın
- [ ] `/usage`-da **iki ayrı sayğac** göstərilir (esse + dərs), `null` halı idarə olunur
- [ ] Dərs siyahısı + `search` / `studentId` / `groupId` filtrləri
- [ ] Silmə → `204`, siyahıdan çıxarılır
- [ ] PDF: bütün slaydlar + test sualları və cavabları (frontend tərəfdə)
