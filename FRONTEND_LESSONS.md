# EssayCheck AI — Mövzu İzahı (Dərs) Funksiyası — Frontend Sənədi

**Bu sənəd 2026-08-23-də yenidən yazılıb — model kökündən dəyişib.** İlk versiya (dərslərin
istifadəçiyə aid olduğu model) heç vaxt frontend tərəfindən istifadə olunmayıb, ona görə bu, breaking
dəyişiklik deyil, sadəcə ilk düzgün spesifikasiyadır. Köhnə versiyanı görmüsünüzsə unudun — aşağıdakı
hər şey canlı sistemdə test olunub.

---

## 1. Konsepsiya: dərslər ORTAQ kitabxanadır

Dərs artıq **istifadəçiyə aid deyil** — bütün müəllimlərin gördüyü ümumi kitabxananın bir sətridir.

**Səbəb:** eyni mövzunu (məs. "Present Perfect") hər müəllim ayrıca yaratsa, hər dəfə AI çağırılır və
token xərclənir. Bunun əvəzinə: **bir mövzu = bir dərs, hamı üçün ortaq.** Müəllim X "Present
Perfect" yaradıbsa, müəllim Y onu dərhal, pulsuz, limitsiz açıb oxuya bilər — heç bir AI çağırışı
olmadan.

- **`studentId` sahəsi tamamilə çıxarılıb.** Dərs artıq bir şagirdə bağlana bilməz — o, ortaq
  resursdur, konkret şagirdin fərdi materialı deyil.
- **Silmə endpoint-i yoxdur.** Bir müəllimin sildiyi dərs qalan 50 müəllimi də ondan məhrum edərdi.
- **`grade` (sinif) artıq MƏCBURİDİR.** Əvvəlki versiyada şagird kartından avtomatik doldurulurdu —
  indi şagird bağlantısı olmadığı üçün bu mümkün deyil, istifadəçi açıq seçməlidir.

---

## 2. Endpoint-lər

Hamısı `[Authorize]` — mövcud `Authorization: Bearer {accessToken}` başlığı kifayətdir.

| Metod | Yol | Nə edir | Limit xərcləyir? |
|---|---|---|---|
| `POST` | `/api/lessons` | Mövzu üzrə dərs açır (kitabxanada varsa qaytarır, yoxdursa yaradır) | Yalnız **həqiqətən yeni** mövzu üçün |
| `GET` | `/api/lessons` | Bütün kitabxana (səhifələnmiş, filtrlənə bilən) | ❌ Yox |
| `GET` | `/api/lessons/{id}` | Tək dərsin tam məzmunu — kim yaratmasından asılı olmayaraq | ❌ Yox |

`DELETE` **yoxdur** — bunu UI-da təklif etməyin.

### 2.1. `POST /api/lessons`

```json
{ "topic": "Present Perfect", "grade": "Grade11" }
```

- `topic` — məcburi, maksimum 200 simvol.
- `grade` — **məcburi**: `"Grade9"` və ya `"Grade11"`. Göndərilməzsə → `400`:
  ```json
  { "succeeded": false, "message": "Sinif seçilməlidir.", "errors": ["Sinif seçilməlidir."] }
  ```
  (Bu, DataAnnotations validasiyasından gəlir, ona görə `AuthResult` formatındadır — adi
  `{ "message": "..." }` formatı deyil. Digər bütün `/api/lessons` xətaları isə sadə formatdadır,
  §5-ə baxın.)

Cavab həmişə `200` + §3-dəki `LessonResponse` — mövzu kitabxanada olsun ya olmasın, **format eynidir**.
Frontend-in bu ikisini fərqləndirməsinə ehtiyac yoxdur; fərq yalnız sürətdə görünür (§6).

### 2.2. `GET /api/lessons` — kitabxana

Query: `search`, `grade` (`Grade9`/`Grade11`), `mine` (bool, default `false`), `page`, `pageSize`.

- `mine=true` — yalnız **cari istifadəçinin özünün yaratdığı** dərslər.
- `mine=false` (default) — kitabxananın **hamısı**, kim yaratmasından asılı olmayaraq.

```json
{
  "items": [
    {
      "id": 13,
      "topic": "Conditional Sentences",
      "grade": "Grade11",
      "createdByName": "Aygün Məmmədova",
      "isMine": false,
      "slideCount": 7,
      "createdAt": "2026-08-22T22:19:23.5Z"
    }
  ],
  "totalCount": 4, "page": 1, "pageSize": 20, "totalPages": 1
}
```

`createdByName` — dərsi ilk yaradan müəllimin adı (kredit üçün, "kim əlavə etdi" göstərmək üçün).
`isMine` — cari istifadəçi özü yaradıbmı. **Heç bir səlahiyyət fərqi yaratmır** — yalnız UI nişanı
üçündür ("Sizin yaratdığınız" etiketi kimi). Slaydların məzmunu qaytarılmır, yalnız `slideCount`.

---

## 3. `LessonResponse` — tam məzmun

```json
{
  "id": 13,
  "topic": "Present Perfect",
  "grade": "Grade11",
  "createdByName": "Aygün Məmmədova",
  "isMine": false,
  "createdAt": "2026-08-22T22:19:23.5Z",
  "slides": [
    {
      "type": "Intro",
      "title": "Present Perfect Tense",
      "body": "Present Perfect zamanı, keçmişdə baş vermiş, lakin indiki zamana təsiri olan hadisələri ifadə etmək üçün istifadə olunur. Bu zaman, xüsusilə yazılı imtahanlarda, fikirlərinizi dəstəkləmək və ya təcrübələrinizi izah etmək üçün çox vacibdir. Məsələn, \"I have visited Paris\" cümləsi, Parisə səyahət etdiyinizi, amma bu təcrübənin indiki zamana təsir etdiyini göstərir. Bu zamanın düzgün istifadəsi, imtahanlarda daha yüksək qiymət almağınıza kömək edə bilər.",
      "formula": null, "keywords": [], "examples": [], "mistakes": [], "comparison": null, "points": []
    },
    {
      "type": "Rule",
      "title": "Present Perfect Qaydasını Anlamaq",
      "body": "Present Perfect, keçmişdə baş vermiş və indiki zamana təsiri olan hadisələri ifadə edir. Bu zamanın forması \"have/has + V3\" şəklindədir. Məsələn, \"I have finished my homework\" cümləsində, ev tapşırığının tamamlandığı, lakin bunun indiki zamana təsiri olduğu bildirilir. Diqqət et, \"have\" istifadə edərkən, subyektin şəxsini nəzərə almalısan: \"I/You/We/They\" üçün \"have\", \"He/She/It\" üçün isə \"has\" istifadə olunur.",
      "formula": "have / has + V3",
      "keywords": ["have", "has", "done", "seen", "ever", "never", "just", "yet"],
      "examples": [], "mistakes": [], "comparison": null, "points": []
    },
    {
      "type": "Examples",
      "title": "Nümunə Cümlələr",
      "body": null, "formula": null, "keywords": [],
      "examples": [
        { "en": "I have completed my project on time.", "az": "Mən layihəmi vaxtında tamamlamışam.", "highlight": "have completed" },
        { "en": "She has never traveled abroad before.", "az": "O, əvvəllər heç vaxt xaricə səyahət etməyib.", "highlight": "has never traveled" }
      ],
      "mistakes": [], "comparison": null, "points": []
    },
    {
      "type": "Mistakes",
      "title": "Səhvlər",
      "body": "Azerbaijani öyrənənlər, Present Perfect zamanını istifadə edərkən tez-tez səhvlər edirlər...",
      "formula": null, "keywords": [], "examples": [],
      "mistakes": [
        { "wrong": "I seen that movie.", "correct": "I have seen that movie.", "note": "Bu cümlədə \"seen\" əvəzinə \"have seen\" istifadə olunmalıdır." }
      ],
      "comparison": null, "points": []
    },
    {
      "type": "Compare",
      "title": "Present Perfect vs. Past Simple",
      "body": null, "formula": null, "keywords": [], "examples": [], "mistakes": [],
      "comparison": {
        "leftTitle": "Present Perfect",
        "leftBody": "Keçmişdə baş vermiş və indiki zamana təsiri olan hadisələri ifadə edir. Məsələn, \"I have visited Paris\" cümləsi, Parisə səyahət etdiyinizi bildirir, lakin bu təcrübənin indiki zamana təsiri var.",
        "rightTitle": "Past Simple",
        "rightBody": "Keçmişdə baş vermiş və artıq tamamlanmış hadisələri ifadə edir. Məsələn, \"I visited Paris last year\" cümləsi, keçən il səyahət etdiyinizi bildirir, lakin bu hadisənin indiki zamana təsiri yoxdur."
      },
      "points": []
    },
    {
      "type": "Summary",
      "title": "Xülasə",
      "body": null, "formula": null, "keywords": [], "examples": [], "mistakes": [], "comparison": null,
      "points": [
        "Present Perfect, keçmişdə baş vermiş hadisələri indiki zamana bağlayır.",
        "\"have/has + V3\" forması ilə istifadə olunur.",
        "\"ever\" və \"never\" kimi sözlər bu zamanla tez-tez istifadə olunur."
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

✅ **Bütün 9 slayd sahəsi hər slaydda var** — istifadə olunmayanlar `null` və ya boş massivdir.
"Sahə mövcuddurmu?" yoxlaması lazım deyil.

✅ **`type` yalnız bu 6 dəyərdən biridir:** `Intro`, `Rule`, `Examples`, `Mistakes`, `Compare`, `Summary`.

✅ **`title` heç vaxt boş deyil.**

✅ **`quiz[].correctIndex` həmişə `0 ≤ correctIndex < options.length`** — kənarda olan sual
backend-də silinir. `options[correctIndex]` əlavə yoxlamadan işlədilə bilər.

✅ **Slayd izahları (`body`) məzmunludur** — hədəf 5-8 cümlə, ~450-550 simvol. Boş/ümumi cümlə
("bu mövzu vacibdir") artıq baş vermir; hər `body` qaydanın niyə belə işlədiyini, real nümunəni,
bir nüansı ehtiva edir. (2026-08-23 tarixli düzəlişdən sonra ölçülüb.)

### 3.2. Zəmanət OLMAYAN (AI-dan asılı)

⚠️ **Slayd sayı** — hədəf 6-8-dir, zəmanət deyil. Slaydları **gələn sıra ilə** göstərin, `type`-a
görə yenidən sıralamayın.

⚠️ **Test sualı sayı** — hədəf 3-dür, sınıq sual atıldığı üçün 2 də ola bilər.

⚠️ **`examples[].highlight`** — tapılmasa `null` gəlir, sadəcə vurğulamayın.

---

## 4. Mini test

- `options` — adətən 4 variant.
- `correctIndex` — həmişə etibarlıdır (§3.1).
- `explanation` — cavab verildikdən sonra göstərilir.

### 4.1. Variantları YENİDƏN QARIŞDIRMAYIN

Backend variantları **artıq** qarışdırıb (deterministik dövri sürüşdürmə) — səbəb: model düzgün
cavabı demək olar həmişə 1-ci variantda verirdi. Frontend əlavə `shuffle()` etsə, eyni dərs hər
açılışda fərqli sırada görünər (kitabxanada ortaq olduğu üçün bu, HƏR istifadəçi üçün uyğunsuzluq
yaradar). Variantları **gəldiyi sıra ilə** göstərin.

---

## 5. Limit — necə işləyir

| Plan | Gündəlik YENİ dərs |
|---|---|
| Free | 1 |
| Pro | 1 |
| ProPlus | 1 |

**Bütün planlarda eynidir — bu, qəsdəndir.** Limit dərsə **giriş** hüququnu deyil, yalnız **yeni AI
çağırışını** məhdudlaşdırır. Kitabxanadakı istənilən sayda dərsi istənilən plan **limitsiz** oxuyur.
ProPlus istifadəçisi də gündə yalnız 1 **yeni** mövzu yarada bilər — amma köhnə mövzuları limitsiz aça
bilər.

### 5.1. Limit nə vaxt xərclənir

| Hal | Limit | AI çağırılır? | Sürət |
|---|---|---|---|
| Mövzu kitabxanada **artıq var** (kim yaradıbsa fərqi yoxdur) | ❌ xərclənmir | ❌ yox | **~1 saniyə** |
| Mövzu kitabxanada **yoxdur** | ✅ xərclənir | ✅ bəli | **15-20 saniyə** |
| Mövzu İngilis dilinə aid deyil (`422`) | ❌ xərclənmir | ✅ bəli (rədd cavabı) | ~2-3 saniyə |
| İki istifadəçi eyni YENİ mövzunu eyni anda yazsa | Yalnız **birincinin** limiti xərclənir, o dərsi yaradır; ikincinin sorğusu həmin dərsi tapıb qaytarır, limiti toxunulmur | — | — |

> **Praktik nəticə:** istifadəçi mövzu yazmazdan əvvəl axtarsa (`GET /api/lessons?search=...`) və
> tapılırsa, birbaşa `/api/lessons/{id}`-ə keçib limitini heç vaxt xərcləməyə bilər. UI axınında
> bunu təşviq etmək faydalıdır (§7).

### 5.2. `/api/subscription/usage`

```json
{
  "plan": "Free", "unlimited": false, "dailyLimit": 1, "usedToday": 0, "remaining": 1,
  "resetAtUtc": "2026-08-24T00:00:00Z",

  "lessonUnlimited": false,
  "lessonDailyLimit": 1,
  "lessonsUsedToday": 0,
  "lessonRemaining": 1
}
```

İki sayğac **tam ayrıdır** — esse və dərs sahələrini ekranda ayrı göstərin. `lessonUnlimited: true`
olarsa `lessonDailyLimit`/`lessonRemaining` `null` gəlir (hazırda heç bir planda baş vermir, amma
kodun gələcəyə açıq saxlanması üçün `null` halını idarə edin).

---

## 6. Xəta halları

| Status | Nə vaxt | Format |
|---|---|---|
| `400` | `grade` göndərilməyib | `AuthResult`: `{ "succeeded": false, "message": "Sinif seçilməlidir.", "errors": [...] }` |
| `422` | Mövzu İngilis dilinə aid deyil | `{ "message": "Bu mövzu İngilis dili dərsinə aid deyil. İngilis dili ilə bağlı mövzu yazın." }` |
| `429` | Gündəlik YENİ dərs limiti bitib | `{ "message": "Bugünkü dərs limitiniz (1) bitib. Sabah yenilənəcək və ya planınızı yüksəldin." }` |
| `404` | Dərs mövcud deyil (`GET /api/lessons/{id}`) | `{ "message": "Dərs tapılmadı." }` |
| `503` / `502` | AI əlçatmazdır | mövcud esse axınındakı mesaj |

`404` burada "yad dərs" demək **deyil** — kitabxanada sahiblik yoxlaması yoxdur, hər `id` hər
istifadəçiyə açıqdır. `404` yalnız `id` real olaraq mövcud olmadıqda gəlir.

---

## 7. Təklif olunan UI axını

1. **"Dərslər" bölməsi** = kitabxana siyahısı (`GET /api/lessons`, `mine=false`). Hər sətirdə
   `createdByName` kiçik etiket kimi ("Aygün Məmmədova yaratdı") — bu, "kim əlavə etdi" hissini verir
   və istifadəçiyə niyə pulsuz olduğunu izah edir.
2. Axtarış qutusu: `search` ilə mövzu yazılanda canlı nəticə göstərin. Tapılarsa — birbaşa aç
   (pulsuz). Tapılmazsa — "Yeni dərs yarat" düyməsi görünsün (limitli, `POST`).
3. **Yeni dərs formasında** `grade` seçimi **məcburidir** (dropdown, defolt yoxdur).
4. Yaratma zamanı **15-20 saniyəlik yükləmə vəziyyəti** göstərin (skeleton slayd və ya progress
   animasiyası) — istifadəçi bunun nə qədər çəkəcəyini bilməlidir.
5. `mine=true` filtri ilə "Mənim yaratdıqlarım" tab-ı əlavə edə bilərsiniz (opsional, `isMine`
   bayrağı ilə eyni məlumatı verir).
6. Silmə düyməsi **olmasın** — funksiya yoxdur.

---

## 8. Məzmun keyfiyyəti — dürüst qeyd

`gpt-4o-mini` işlədilir (ucuz model). 2026-08-23 tarixli ölçmə:

- ✅ **İzah artıq məzmunludur** (§3.1) — əvvəlki versiyada bir cümlə idi, indi 5-8 cümlə, konkret
  detal var.
- ✅ **Struktur etibarlıdır** — slaydlar, sahələr, `highlight`, test formatı problemsiz gəlir.
- ⚠️ **`Grade9` və `Grade11` fərqi hələ də zəifdir** — nümunələr demək olar eynidir. Sinif seçimini
  UI-da göstərin, amma "9-cu sinif üçün xüsusi hazırlanıb" kimi güclü vəd verməyin.

Model dəyişdirilməsi backend tərəfdə bir sətir konfiqurasiyadır — keyfiyyət problem olarsa deyin.

---

## 9. Yekun yoxlama siyahısı (frontend üçün)

- [ ] "Dərslər" = **ortaq kitabxana** ekranı, "mənim dərslərim" DEYİL
- [ ] `createdByName` + `isMine` göstərilir (yalnız informativ, giriş fərqi yaratmır)
- [ ] Yeni dərs formasında `grade` **məcburi seçim**, defolt yoxdur
- [ ] Yaratma 15-20 saniyə çəkə bilər — yükləmə vəziyyəti var
- [ ] Mövzu artıq kitabxanadadırsa açılış **~1 saniyədir** — bunu istifadəçiyə izah etməyə ehtiyac yoxdur
- [ ] Test: `options[correctIndex]` birbaşa işlədilir, **əlavə qarışdırma yoxdur**
- [ ] `422` və `429` mesajları göstərilir; `400` (`grade` yoxdur) `AuthResult` formatında oxunur
- [ ] **Silmə düyməsi yoxdur** — bu funksiya təqdim olunmayıb
- [ ] `/usage`-da iki ayrı sayğac (esse + dərs), `null` halı idarə olunur
- [ ] Kitabxana filtrləri: `search`, `grade`, `mine`
