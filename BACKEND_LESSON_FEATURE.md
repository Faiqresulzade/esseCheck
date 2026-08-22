# EssayCheck AI — Mövzu İzahı (Dərs) Funksiyası — Backend Sifarişi

Bu sənəd **frontend tərəfindən backend-ə sifarişdir**: mobil tətbiqə "mövzu izahı" funksiyası əlavə olunur və bunun üçün yeni endpoint-lər lazımdır. Frontend hazır olan kimi bu kontrakta uyğun qurulacaq.

**Xülasə:** İstifadəçi mövzu adını yazır (məs. *Present Perfect*) → AI 6-8 slaydlıq dərs hazırlayır → tətbiq slaydları animasiya ilə göstərir, ingilis nümunələrini səsləndirir, sonda 3 suallıq mini test verir → dərs saxlanılır və PDF kimi paylaşılır.

**Bu, breaking dəyişiklik deyil** — mövcud endpoint-lərə toxunulmur, yalnız `/api/subscription/usage` cavabına yeni sahələr **əlavə olunur** (§5).

---

## 1. Konsepsiya və qərarlar

| Mövzu | Qərar |
|---|---|
| Əhatə | **Yalnız İngilis dili**: qrammatika, leksika, esse yazma texnikası |
| İzah dili | **Azərbaycanca**; nümunə cümlələr ingiliscə + azərbaycanca tərcümə |
| Səviyyə | `Grade9` / `Grade11` — AI izahın dərinliyini buna uyğunlaşdırır |
| Kim istifadə edir | **Hər kəs** (müəllim də, şagird də). Plan tələbi yoxdur, yalnız gündəlik limit |
| Şagird bağlantısı | Opsional `studentId` — dərs həmin şagird üçün qeyd olunur |
| Limit | Esse limitindən **AYRI** gündəlik sayğac (§5) |
| Saxlama | Dərslər bazada saxlanılır; təkrar baxış **limit xərcləmir** |
| Paylaşma | PDF — **frontend özü yaradır**, backend-dən fayl tələb olunmur |

---

## 2. Endpoint-lər

Hamısı `[Authorize]` altındadır — mövcud `Authorization: Bearer {accessToken}` başlığı kifayətdir.

| Metod | Yol | Təsvir | Limit xərcləyir? |
|---|---|---|---|
| `POST` | `/api/lessons` | Yeni dərs yaradır (AI çağırışı) | ✅ Bəli |
| `GET` | `/api/lessons` | Saxlanmış dərslər (səhifələnmiş) | ❌ Yox |
| `GET` | `/api/lessons/{id}` | Tək dərsin tam məzmunu | ❌ Yox |
| `DELETE` | `/api/lessons/{id}` | Dərsi silir | ❌ Yox |

**Sahiblik:** İstifadəçi yalnız öz dərslərini görür. Başqasının dərsinə müraciət **`404`** qaytarır (`403` yox) — `FRONTEND_TEACHER_GROUPS.md` §2.3-dəki eyni prinsip.

---

### 2.1. `POST /api/lessons` — dərs yaratmaq

**Sorğu:**
```json
{
  "topic": "Present Perfect",
  "grade": "Grade11",
  "studentId": 4
}
```

- `topic` — **məcburi**, maksimum 200 simvol.
- `grade` — `"Grade9"` | `"Grade11"`. **Opsional**: göndərilməyibsə və `studentId` verilibsə, şagirdin kartındakı sinif işlədilir (esse endpoint-indəki eyni məntiq). Heç biri yoxdursa → `400 { "message": "Sinif seçilməlidir." }`.
- `studentId` — **opsional**. Mövcud deyilsə/başqasınındırsa → `400 { "message": "Şagird tapılmadı." }` və **kvota sərf edilmir** (AI çağırılmır).

**Cavab (200):** §3-dəki `LessonResponse`.

---

### 2.2. `GET /api/lessons` — siyahı

**Query:** `search` (opsional, mövzuya görə), `studentId` (opsional), `page` (default 1), `pageSize` (default 20).

```json
{
  "items": [
    {
      "id": 12,
      "topic": "Present Perfect",
      "grade": "Grade11",
      "studentId": 4,
      "studentName": "Əli Məmmədov",
      "slideCount": 8,
      "createdAt": "2026-08-22T10:15:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

Siyahıda **slaydların tam məzmunu qaytarılmır** (yüngül olsun deyə) — yalnız `slideCount`.

---

### 2.3. `GET /api/lessons/{id}` və `DELETE /api/lessons/{id}`

- `GET` → §3-dəki tam `LessonResponse`, ya da `404`.
- `DELETE` → `204 No Content`, ya da `404`.

---

## 3. `LessonResponse` — dərsin strukturu

```json
{
  "id": 12,
  "topic": "Present Perfect",
  "grade": "Grade11",
  "studentId": 4,
  "studentName": "Əli Məmmədov",
  "createdAt": "2026-08-22T10:15:00Z",
  "slides": [
    {
      "type": "Intro",
      "title": "Present Perfect nə üçün lazımdır?",
      "body": "Bu zaman keçmişdə başlamış və indi ilə əlaqəsi olan hadisələri bildirir. İmtahan esselərində ən çox işlədilən zamanlardandır.",
      "formula": null, "keywords": [], "examples": [], "mistakes": [], "comparison": null, "points": []
    },
    {
      "type": "Rule",
      "title": "Qayda",
      "body": "Köməkçi feil şəxsə görə dəyişir, əsas feil isə həmişə III formada qalır.",
      "formula": "have / has + V3",
      "keywords": ["have", "has", "V3", "already", "yet", "since", "for"],
      "examples": [], "mistakes": [], "comparison": null, "points": []
    },
    {
      "type": "Examples",
      "title": "Nümunələr",
      "body": null,
      "formula": null, "keywords": [],
      "examples": [
        { "en": "I have finished my homework.", "az": "Mən ev tapşırığımı bitirmişəm.", "highlight": "have finished" },
        { "en": "She has lived here for five years.", "az": "O, beş ildir burada yaşayır.", "highlight": "has lived" }
      ],
      "mistakes": [], "comparison": null, "points": []
    },
    {
      "type": "Mistakes",
      "title": "Tez-tez edilən səhvlər",
      "body": null,
      "formula": null, "keywords": [], "examples": [],
      "mistakes": [
        { "wrong": "I have finish my work.", "correct": "I have finished my work.", "note": "Əsas feil III formada olmalıdır." },
        { "wrong": "I have seen him yesterday.", "correct": "I saw him yesterday.", "note": "Konkret keçmiş vaxt varsa Past Simple işlədilir." }
      ],
      "comparison": null, "points": []
    },
    {
      "type": "Compare",
      "title": "Past Simple ilə fərq",
      "body": null,
      "formula": null, "keywords": [], "examples": [], "mistakes": [],
      "comparison": {
        "leftTitle": "Present Perfect",
        "leftBody": "Nəticə indi əhəmiyyətlidir, vaxt dəqiq deyil: I have lost my key.",
        "rightTitle": "Past Simple",
        "rightBody": "Konkret keçmiş vaxt bildirilir: I lost my key yesterday."
      },
      "points": []
    },
    {
      "type": "Summary",
      "title": "Xülasə",
      "body": null,
      "formula": null, "keywords": [], "examples": [], "mistakes": [], "comparison": null,
      "points": [
        "have/has + V3 formulunu yadda saxla.",
        "Konkret keçmiş vaxt varsa Past Simple işlət.",
        "already, yet, since, for sözləri bu zamanın işarəsidir."
      ]
    }
  ],
  "quiz": [
    {
      "question": "She ___ her homework already.",
      "options": ["finish", "has finished", "have finished", "finished"],
      "correctIndex": 1,
      "explanation": "Üçüncü şəxs tək olduğu üçün has, əsas feil isə III formada: has finished."
    }
  ]
}
```

### 3.1. Slayd qaydaları

- **Slayd sayı: 6-8.** Ardıcıllıq: `Intro` → `Rule` → `Examples` (1-2 ədəd) → `Mistakes` → `Compare` → `Summary`.
- `type` dəyərləri **yalnız bunlar** ola bilər: `"Intro"`, `"Rule"`, `"Examples"`, `"Mistakes"`, `"Compare"`, `"Summary"`.
- ⚠️ **Bütün sahələr həmişə mövcud olmalıdır** — istifadə olunmayanlar `null` və ya boş massiv kimi qaytarılsın (frontend-də yoxlama sadələşsin deyə, sahə "yoxdur" halı olmasın).
- `title` hər slaydda **məcburidir**, boş olmamalıdır.

### 3.2. Sahələrin izahı

| Sahə | Hansı slaydda | Təsvir |
|---|---|---|
| `body` | Intro, Rule, Compare | 1-3 cümləlik izah mətni (azərbaycanca) |
| `formula` | Rule | Qısa formul, məs. `"have / has + V3"` |
| `keywords` | Rule | Açar sözlər — tətbiqdə ardıcıl işıqlandırılacaq, **maksimum 8 ədəd** |
| `examples[].en` | Examples | İngilis cümləsi — **tətbiq bunu səsləndirəcək**, ona görə düzgün, tam cümlə olmalıdır |
| `examples[].az` | Examples | Azərbaycanca tərcümə |
| `examples[].highlight` | Examples | `en` cümləsinin **hərfi hərfinə** alt-sətri (məs. `"have finished"`) — vurğulanacaq hissə. Cümlədə tapılmasa frontend sadəcə vurğulamır |
| `mistakes[]` | Mistakes | `wrong` / `correct` / `note` — **maksimum 3 ədəd** |
| `comparison` | Compare | İki sütunlu müqayisə |
| `points[]` | Summary | 3 qısa yekun cümləsi |

### 3.3. Mini test (`quiz`)

- **Dəqiq 3 sual.** Hər sualda **4 variant**.
- `correctIndex` — 0-dan başlayan indeks (0-3).
- `explanation` — cavab verildikdən sonra göstərilir, 1-2 cümlə, azərbaycanca.
- Suallar dərsdə keçilən materiala əsaslanmalıdır (dərsdə olmayan mövzudan sual verilməsin).

### 3.4. Səviyyəyə uyğunlaşma

| Sinif | Gözlənti |
|---|---|
| `Grade9` | Sadə dil, qısa cümlələr, gündəlik nümunələr (A2-B1 səviyyəsi). Terminlər azərbaycanca izah olunur |
| `Grade11` | Daha dərin izah, imtahan/esse kontekstinə uyğun nümunələr (B1-B2), qarışdırılan hallara diqqət |

---

## 4. Mövzu yoxlaması

Mövzu **İngilis dili ilə bağlı olmalıdır** (qrammatika, leksika, esse yazma, tələffüz və s.).

Mövzu uyğun deyilsə (məs. "inteqral hesablama", "Səfəvilər dövləti", mənasız simvollar) → **AI dərs yaratmamalı**, əvəzinə:

```
422 { "message": "Bu mövzu İngilis dili dərsinə aid deyil. İngilis dili ilə bağlı mövzu yazın." }
```

⚠️ Bu halda **kvota sərf edilməməlidir** — mövzu yoxlaması ya ucuz/ayrıca addımda, ya da limit sayğacı artırılmadan əvvəl aparılsın.

---

## 5. Limit — AYRI gündəlik sayğac

Dərs yaratmaq esse yoxlamadan **ayrı** sayılır. Təklif olunan hədlər:

| Plan | Gündəlik dərs |
|---|---|
| Free | 1 |
| Pro | 5 |
| ProPlus | Limitsiz |

Sıfırlanma vaxtı esse limiti ilə eyni olsun (mövcud `resetAtUtc` məntiqi).

### 5.1. `/api/subscription/usage` cavabına əlavələr

Mövcud sahələr **dəyişmir**, üzərinə əlavə olunur:

```json
{
  "plan": "Pro",
  "unlimited": false,
  "dailyLimit": 10,
  "usedToday": 3,
  "remaining": 7,
  "resetAtUtc": "2026-08-23T00:00:00Z",

  "lessonUnlimited": false,
  "lessonDailyLimit": 5,
  "lessonsUsedToday": 1,
  "lessonRemaining": 4
}
```

- `lessonUnlimited: true` olanda `lessonDailyLimit` və `lessonRemaining` `null` ola bilər (esse sayğacındakı eyni məntiq).
- Limit bitibsə `POST /api/lessons` → **`429`** (esse ilə eyni status), mesaj gündəlik dərs limitini bildirsin.

### 5.2. Plan məlumatlarına əlavə (opsional)

`/api/subscription/plans` cavabındakı `features[]` siyahısına dərs limitini əks etdirən sətir əlavə olunsa yaxşı olar (məs. `"Gündə 5 mövzu izahı"`) — abunə ekranında göstərilsin deyə.

---

## 6. Keş (tövsiyə, məcburi deyil)

Eyni `topic` + `grade` cütü üçün əvvəllər yaradılmış dərs varsa, AI-ı yenidən çağırmaq əvəzinə hazır məzmunu kopyalamaq olar (`topic` normalizasiyası: boşluqların təmizlənməsi + kiçik hərflər).

- Bu, **AI xərcini ciddi azaldır** (eyni populyar mövzular təkrar-təkrar soruşulacaq).
- Keşdən gəlsə belə **limit sayğacı artırılsın** — sadə və proqnozlaşdırıla bilən davranış olsun, sui-istifadənin qarşısı alınsın.
- Dərs hər istifadəçi üçün ayrıca sətir kimi saxlanılır (öz siyahısında görünsün deyə).

---

## 7. Xəta halları — yekun cədvəl

| Status | Nə vaxt | Mesaj nümunəsi | Kvota sərf olunur? |
|---|---|---|---|
| `400` | Sinif yoxdur | `"Sinif seçilməlidir."` | ❌ |
| `400` | Şagird tapılmadı | `"Şagird tapılmadı."` | ❌ |
| `422` | Mövzu İngilis dilinə aid deyil | `"Bu mövzu İngilis dili dərsinə aid deyil..."` | ❌ |
| `429` | Gündəlik dərs limiti bitib | `"Bugünkü dərs limitiniz bitib."` | — |
| `404` | Dərs yoxdur / başqasınındır | `"Dərs tapılmadı."` | — |
| `502` / `503` | AI xidməti əlçatmazdır | mövcud esse axınındakı mesaj | ❌ |

---

## 8. Frontend nə edəcək (backend-dən əlavə heç nə lazım deyil)

Aşağıdakılar tamamilə mobil tərəfdə həll olunur, backend-ə iş düşmür:

- **Animasiyalar** — slaydların keçidi, mətnin yazılma effekti, `keywords` işıqlanması, progress bar
- **Səsləndirmə** — `examples[].en` cümlələri cihazın daxili TTS mühərriki ilə oxunur (ingilis səsi; azərbaycan mətni səsləndirilmir)
- **Mini test qarşılıqlılığı** — variant seçimi, dərhal yoxlama, nəticə ekranı
- **PDF** — bütün slaydlar + test sualları və cavabları statik sənəd kimi yaradılır və paylaşılır
- **Mövzu təklifləri** — populyar mövzuların siyahısı tətbiqin içindədir, endpoint lazım deyil
- **Zəif tərəfə görə təklif** — mövcud `/api/analytics/students/{id}` cavabındakı `weakestDirection` və `weaknesses[]` işlədilir, yeni endpoint lazım deyil

---

## 9. Yoxlama siyahısı (backend üçün)

- [ ] `POST /api/lessons` — `topic` + opsional `grade`/`studentId`, §3-dəki strukturda cavab
- [ ] `GET /api/lessons` — səhifələnmiş siyahı (`search`, `studentId` filtrləri), slayd məzmunu olmadan
- [ ] `GET /api/lessons/{id}` — tam məzmun, **limit xərcləmir**
- [ ] `DELETE /api/lessons/{id}` — `204` / `404`
- [ ] Sahiblik: başqasının dərsi → `404`
- [ ] Slaydlar: 6-8 ədəd, yalnız icazə verilən `type` dəyərləri, bütün sahələr həmişə mövcud (`null`/boş massiv)
- [ ] `examples[].en` düzgün, tam ingilis cümləsidir (səsləndiriləcək)
- [ ] `examples[].highlight` `en` cümləsinin hərfi alt-sətridir
- [ ] `quiz` — dəqiq 3 sual, hər birində 4 variant, `correctIndex` 0-3 aralığında
- [ ] `Grade9` / `Grade11` üçün izahın dərinliyi fərqlidir
- [ ] Uyğunsuz mövzu → `422`, kvota sərf edilmir
- [ ] Ayrı gündəlik dərs limiti işləyir, bitəndə `429`
- [ ] `/api/subscription/usage` cavabına `lesson*` sahələri əlavə olunub
- [ ] (Opsional) `topic` + `grade` üzrə keş — AI xərcini azaltmaq üçün
