# EssayCheck AI — Qrup / Şagird Funksiyası (Müəllim Rejimi)

Bu sənəd backend-ə yeni əlavə olunan **qrup və şagird idarəetməsini** izah edir. Bu, **breaking dəyişiklik deyil** — mövcud `/api/essay/*` endpoint-ləri əvvəlki kimi işləyir, yalnız üzərinə opsional sahələr əlavə olunub. Köhnə frontend kodu heç nə dəyişdirmədən işləməyə davam edir; bu sənəd yeni funksiyanı necə əlavə edəcəyinizi göstərir.

---

## 1. Konsepsiya

İstifadəçi (müəllim) **qruplar** yarada bilər (məs. "11-A İngilis"), hər qrupa **şagirdlər** əlavə edə bilər. Esse yoxlayanda **opsional** olaraq hansı şagird üçün olduğunu seçir. Sonra tarixçədə "kimin essesi" görünür.

**Vacib qeydlər:**
- **Ayrıca "müəllim" rolu yoxdur.** İstənilən istifadəçi qrup/şagird yarada bilər — heç bir plan tələbi, heç bir təsdiq prosesi yoxdur.
- **Şagirdin öz hesabı yoxdur.** Login etmir, e-mail lazım deyil. Sadəcə müəllimin siyahısındakı bir addır.
- **Şagird seçimi tamamilə opsionaldır.** Müəllim əvvəlki kimi (şagird seçmədən) də esse yoxlaya bilər — bu, əvvəlki bütün axını sındırmır.
- Qrup yaratmaq/şagird əlavə etmək **pulsuzdur**, plan yoxlanmır. Yalnız gündəlik esse limiti (bax `FRONTEND_UNIFIED_PLAN_LIMITS.md`) əvvəlki kimi işləyir — deməli, şagird üçün yoxlamaq da adi bir yoxlamadır və limitə sayılır.

---

## 2. Yeni endpoint-lər

Bütün endpoint-lər `[Authorize]` altındadır — mövcud `Authorization: Bearer {accessToken}` başlığı kifayətdir, yeni auth axını yoxdur.

### 2.1. Qruplar — `/api/groups`

| Metod | Yol | Təsvir |
|---|---|---|
| `GET` | `/api/groups` | Müəllimin bütün qrupları |
| `POST` | `/api/groups` | Yeni qrup |
| `PUT` | `/api/groups/{id}` | Qrupun adını dəyiş |
| `DELETE` | `/api/groups/{id}` | Qrupu sil (soft-delete) |
| `GET` | `/api/groups/{id}/students` | Bu qrupun şagirdləri |
| `POST` | `/api/groups/{id}/students` | Bu qrupa şagird əlavə et |

**`GET /api/groups`** cavabı:
```json
[
  { "id": 1, "name": "11-A İngilis", "studentCount": 14, "createdAt": "2026-08-19T16:09:58Z" },
  { "id": 2, "name": "9-B Hazırlıq", "studentCount": 8, "createdAt": "2026-08-19T16:09:59Z" }
]
```

**`POST /api/groups`** body:
```json
{ "name": "11-A İngilis" }
```
Uğurlu cavab (200): yeni qrup obyekti (`studentCount: 0`).
Xəta (400): `{ "message": "Maksimum 50 qrup yarada bilərsiniz." }` — hər müəllim üçün sərt hədd.

**`PUT /api/groups/{id}`** eyni body (`{ "name": "..." }`) → `204 No Content`, ya da `404` (qrup tapılmadı / başqasınındır).

**`DELETE /api/groups/{id}`** → `204` / `404`. **Diqqət:** qrupu silmək onun şagirdlərini də siyahıdan çıxarır (soft-delete), amma o şagirdlərin **keçmiş esseləri tarixçədə qalır**.

---

### 2.2. Şagirdlər — `/api/students`

| Metod | Yol | Təsvir |
|---|---|---|
| `GET` | `/api/students` | **Bütün** şagirdlər (droplist üçün əsas endpoint) |
| `GET` | `/api/students?groupId=1` | Yalnız bir qrupdakı şagirdlər |
| `GET` | `/api/students/{id}` | Tək şagird kartı |
| `PUT` | `/api/students/{id}` | Şagirdi yenilə |
| `DELETE` | `/api/students/{id}` | Şagirdi sil (soft-delete) |

**`GET /api/students`** cavabı — droplist üçün bunu işlədin:
```json
[
  { "id": 1, "groupId": 1, "groupName": "11-A İngilis", "fullName": "Əli Məmmədov", "grade": "Grade11", "createdAt": "..." },
  { "id": 2, "groupId": 1, "groupName": "11-A İngilis", "fullName": "Nigar Əliyeva", "grade": null, "createdAt": "..." },
  { "id": 3, "groupId": 2, "groupName": "9-B Hazırlıq", "fullName": "Rəşad Hüseynov", "grade": "Grade9", "createdAt": "..." }
]
```

Droplist-i qrup adına görə qrupla göstərmək üçün `groupName`/`groupId` sahələri kifayətdir — ayrıca sorğuya ehtiyac yoxdur.

**`POST /api/groups/{groupId}/students`** body:
```json
{ "fullName": "Əli Məmmədov", "grade": "Grade11" }
```
- `fullName` məcburidir.
- `grade` **opsionaldır** (`"Grade9"`, `"Grade11"` və ya göndərilməsin/`null`). Təyin olunubsa, esse formasında sinif seçimini əvəz edir (bax §3).

Xəta (400): `{ "message": "Bir qrupda maksimum 200 şagird ola bilər." }`

**`PUT /api/students/{id}`** eyni body ilə tam yeniləmə (ad + sinif) → `204` / `404`.

**`DELETE /api/students/{id}`** → `204` / `404`. Şagird siyahıdan çıxır, **esseləri toxunulmur**.

---

### 2.3. Sahiblik və təhlükəsizlik

Bütün endpoint-lər yalnız **cari istifadəçinin öz** qrup/şagirdlərini görür və dəyişir. Başqa istifadəçinin qrupuna/şagirdinə müraciət **`404 Not Found`** qaytarır (`403` yox) — beləliklə "bu id mövcuddur, amma sənin deyil" məlumatı sızmır. Frontend-də bunu adi "tapılmadı" kimi göstərin.

---

## 3. Esse formasına inteqrasiya

### 3.1. `POST /api/essay/evaluate` — yeni sahələr

```json
{
  "text": "...",
  "title": "My School",
  "source": "Text",
  "grade": "Grade11",
  "topic": null,
  "studentId": 1
}
```

- **`studentId`** (yeni, opsional) — hansı şagird üçün yoxlanır. Boş buraxılsa esse əvvəlki kimi müəllimin öz essesi kimi qeyd olunur.
- **`grade`** artıq **opsionaldır** (əvvəl məcburi idi). Qayda:
  - `grade` göndərilibsə → o işlədilir (şagirdin kartındakı sinifdən asılı olmayaraq).
  - `grade` göndərilməyib, `studentId` göndərilib və o şagirdin kartında sinif təyin olunubsa → **şagirdin sinfi avtomatik işlədilir**.
  - Heç biri yoxdursa → `400 { "message": "Sinif seçilməlidir." }`.

**Frontend üçün praktik nəticə:** şagird seçildikdə, əgər onun kartında sinif varsa, sinif seçim düymələrini (Grade9/Grade11) **gizlətmək və ya avtomatik doldurub read-only göstərmək** olar — istifadəçi təkrar seçmək məcburiyyətində qalmır. Şagirdin kartında sinif yoxdursa, sinif seçimi əvvəlki kimi məcburi görünməlidir.

**Yeni xəta halı:** `studentId` göndərilib, amma mövcud deyil / başqasınındırsa → `400 { "message": "Şagird tapılmadı." }`. Bu, **kvota sərf edilmədən** qayıdır (AI çağırılmır) — istifadəçinin haqqı yanmır.

**9-cu sinif şəkilli forması** (`POST /api/essay/evaluate/grade9-images`, `multipart/form-data`) da eyni məntiqlə **`studentId`** form-field-i qəbul edir (opsional). Bu formada sinif həmişə Grade9 olduğu üçün əlavə `grade` sahəsi yoxdur.

### 3.2. Esse cavabında (`EssayDetailResponse`) yeni sahələr

`POST /api/essay/evaluate`, `GET /api/essay/history/{id}` cavablarına iki sahə əlavə olunub:

```json
{
  "id": 4,
  "title": "My School",
  ...
  "studentId": 1,
  "studentName": "Əli Məmmədov"
}
```

Şagird seçilməyibsə hər ikisi `null`.

---

## 4. Tarixçə (`GET /api/essay/history`)

**Ayrı ekran yoxdur** — həm müəllimin öz esseləri, həm şagird esseləri **eyni siyahıdadır**. Yeni sahələr:

```json
{
  "items": [
    { "id": 4, "title": "My School", "createdAt": "...", "wordCount": 97, "totalScore": 2.2, "grade": "Grade11", "studentId": 1, "studentName": "Əli Məmmədov" },
    { "id": 3, "title": "My Career",  "createdAt": "...", "wordCount": 120, "totalScore": 4.1, "grade": "Grade11", "studentId": null, "studentName": null }
  ],
  "totalCount": 2, "averageScore": 3.15, "page": 1, "pageSize": 20, "totalPages": 1
}
```

Hər sətirdə `studentName` varsa göstərin (məs. bir badge/etiket kimi: "Əli Məmmədov"), yoxdursa heç nə göstərməyin (öz essesi).

**Yeni query parametrləri (opsional, filtr üçün):**
- `GET /api/essay/history?studentId=1` — yalnız bu şagirdin esseləri.
- `GET /api/essay/history?groupId=1` — yalnız bu qrupdakı şagirdlərin esseləri (qrup silinsə belə keçmiş esselər görünür).

Praktik istifadə: "Bu şagirdin bütün esseləri" ekranı üçün ayrıca endpoint qurmağa ehtiyac yoxdur — mövcud tarixçə endpoint-ini `studentId` ilə çağırın.

---

## 5. Təklif olunan UI axını

1. **Yeni "Şagirdlərim" bölməsi** (yan menyu/tab): qrup siyahısı → qrupa klik → şagird siyahısı. Hər ikisi üçün "+" düyməsi (ad, opsional sinif).
2. **Esse yoxlama formasında** mövcud sahələrin üstünə: "Kimin üçün?" seçici (opsional dropdown) — `GET /api/students` ilə doldurulur, qrup adına görə qruplanmış (`groupName`). "Özüm üçün" defolt seçim ola bilər.
3. Şagird seçiləndə, əgər onun `grade` sahəsi doludursa, sinif seçimini avtomatik doldurun/gizlədin (§3.1).
4. **Tarixçə ekranında** hər sətirdə şagird adını kiçik bir etiket kimi göstərin, üstündə isteğe bağlı filtr (qrup/şagird üzrə).
5. Şagird/qrup silmə əməliyyatlarında istifadəçiyə "esse tarixçəsi silinmir, sadəcə siyahıdan çıxır" mesajını göstərin (soft-delete davranışı).

---

## 6. Analitika — `/api/analytics`

Hesabatlar **artıq hazırdır**. Hamısı mövcud esse nəticələrindən hesablanır: **əlavə AI çağırışı yoxdur**, gündəlik limitə **təsir etmir**, pulsuzdur — istədiyiniz qədər çağıra bilərsiniz (yenə də hər ekran açılışında bir dəfə kifayətdir).

| Metod | Yol | Təsvir |
|---|---|---|
| `GET` | `/api/analytics/overview` | Müəllimin ümumi paneli |
| `GET` | `/api/analytics/groups/{groupId}` | Qrup icmalı + şagird sıralaması |
| `GET` | `/api/analytics/students/{studentId}` | Şagird profili: trend + zəif tərəflər |

Yad/silinmiş qrup və ya şagird → `404 { "message": "Qrup tapılmadı." }` (§2.3-dəki eyni prinsip).

### 6.1. Üç ekranda təkrarlanan bloklar

**`scores` — ballar icmalı:**
```json
{
  "total": 2.57,
  "totalPercent": 51.3,
  "directions": [
    { "direction": "Structure",  "average": 0.53, "max": 1, "percent": 53.3 },
    { "direction": "Content",    "average": 1.07, "max": 2, "percent": 53.3 },
    { "direction": "Grammar",    "average": 0.5,  "max": 1, "percent": 50 },
    { "direction": "Vocabulary", "average": 0.47, "max": 1, "percent": 46.7 }
  ]
}
```

> ⚠️ **Ən vacib qayda:** istiqamətlərin maksimumu **fərqlidir** — `Content` 2.0, qalanları 1.0. Diaqramda və müqayisədə **`average` yox, `percent` istifadə edin**. Xam balla sıralasanız `Content` həmişə "ən yaxşı" görünəcək. `average` yalnız "0.53 / 1.0" kimi mətn göstərmək üçündür.

**`weakestDirection`** — faizcə ən aşağı istiqamət: `"Structure" | "Content" | "Grammar" | "Vocabulary"`, esse yoxdursa `null`. "Ən çox işləməli olduğu sahə" kartı üçün birbaşa bunu işlədin.

**`mistakes` — səhv profili:**
```json
{
  "total": 33,
  "averagePerEssay": 11,
  "perHundredWords": 7.3,
  "categories": [
    { "category": "Grammar",           "count": 18, "share": 54.5 },
    { "category": "Spelling",          "count": 6,  "share": 18.2 },
    { "category": "Vocabulary",        "count": 6,  "share": 18.2 },
    { "category": "NaturalExpression", "count": 3,  "share": 10.9 }
  ]
}
```
`share` — pay faizi (pasta diaqramı üçün, cəmi ~100). `perHundredWords` — hər 100 sözə düşən səhv; **şagirdləri müqayisə edərkən bunu işlədin**, xam `total`-ı yox (uzun esse yazan şagird daha çox səhv verir, bu onu pis göstərmir).

**`hasEnoughData`** — ən azı 2 esse varmı. `false` olanda trend qrafiki **çəkilməməlidir** ("Hələ kifayət qədər məlumat yoxdur, ən azı 2 esse lazımdır" yazın). Data yenə də qaytarılır (sıfırlarla), sadəcə mənalı deyil.

### 6.2. Şagird profili — `GET /api/analytics/students/{id}`

```json
{
  "studentId": 4, "fullName": "Ali Aliyev", "groupId": 3, "groupName": "11-A",
  "grade": "Grade11", "essayCount": 3, "hasEnoughData": true,
  "scores": { ... }, "weakestDirection": "Vocabulary",
  "latestTotal": 3.2, "previousTotal": 2.5, "delta": 0.7,
  "mistakes": { ... },
  "trend": [
    { "essayId": 5, "date": "2026-07-01T10:00:00Z", "title": "Ali 1", "wordCount": 150,
      "total": 2, "structure": 0.4, "content": 0.8, "grammar": 0.4, "vocabulary": 0.4, "mistakeCount": 14 },
    { "essayId": 6, "date": "2026-07-08T10:00:00Z", "title": "Ali 2", "wordCount": 150,
      "total": 2.5, "structure": 0.5, "content": 1, "grammar": 0.5, "vocabulary": 0.5, "mistakeCount": 11 },
    { "essayId": 7, "date": "2026-07-15T10:00:00Z", "title": "Ali 3", "wordCount": 150,
      "total": 3.2, "structure": 0.7, "content": 1.4, "grammar": 0.6, "vocabulary": 0.5, "mistakeCount": 8 }
  ],
  "weaknesses": [
    { "text": "Zaman uzlaşmasında səhvlər var.", "count": 3 },
    { "text": "Abzas bölgüsü zəifdir.", "count": 1 }
  ],
  "recommendations": [
    { "text": "Present Perfect qaydasını təkrar et.", "count": 3 }
  ]
}
```

- `trend` — **tarixə görə artan** sırada (ən köhnə → ən yeni), maksimum 100 nöqtə. Qrafikin X oxu `date`, Y oxu `total` (0–5). Nöqtəyə klik → `essayId` ilə esse detalına keçid (`GET /api/essay/history/{id}`).
- `delta` — son esse ilə ondan əvvəlkinin fərqi. Müsbət = irəliləyiş (yaşıl ▲), mənfi = geriləmə (qırmızı ▼), `null` = yalnız 1 esse var.
- `weaknesses` / `recommendations` — **son 10 essenin AI rəyindən** götürülüb, təkrarlanma sayına görə sıralanıb, ən çox 5 ədəd. `count: 3` = "bu qeyd 3 essedə təkrarlanıb" → davamlı problem, ön plana çıxarın (məs. `count > 1` olanları qalın yazın).
  > Qeyd: birləşdirmə **mətn səviyyəsindədir** — AI eyni fikri fərqli sözlərlə yazsa, ayrı sətir kimi görünəcək. `count`-a "təxmini göstərici" kimi yanaşın.

### 6.3. Qrup icmalı — `GET /api/analytics/groups/{id}`

```json
{
  "groupId": 3, "name": "11-A", "studentCount": 3, "essayCount": 5, "hasEnoughData": true,
  "scores": { ... }, "weakestDirection": "Vocabulary", "mistakes": { ... },
  "students": [
    { "studentId": 5, "fullName": "Nigar Mammadova", "rank": 1, "essayCount": 2,
      "averageTotal": 3.25, "latestTotal": 3, "delta": -0.5, "weakestDirection": "Vocabulary" },
    { "studentId": 4, "fullName": "Ali Aliyev", "rank": 2, "essayCount": 3,
      "averageTotal": 2.57, "latestTotal": 3.2, "delta": 0.7, "weakestDirection": "Vocabulary" },
    { "studentId": 6, "fullName": "Zero Essays", "rank": 0, "essayCount": 0,
      "averageTotal": null, "latestTotal": null, "delta": null, "weakestDirection": null }
  ]
}
```

- `students` **artıq sıralanmış** gəlir — olduğu kimi göstərin, yenidən sortlamayın.
- `rank: 0` + `essayCount: 0` = hələ heç bir essesi yoxdur. Bunlar siyahının **sonunda** gəlir; medal/yer nömrəsi əvəzinə "Hələ esse yoxdur" yazın.
- Qrup rəqəmləri **yalnız silinməmiş şagirdlərin** esselərini sayır (ekrandakı siyahı ilə rəqəmlər uyğun gəlsin deyə). Diqqət: `GET /api/essay/history?groupId=` bundan fərqlidir — orada silinmiş şagirdin essesi də tapılır.

### 6.4. Ümumi panel — `GET /api/analytics/overview`

```json
{
  "groupCount": 2, "studentCount": 4, "essayCount": 7,
  "essaysWithStudent": 6, "essaysLast30Days": 3, "hasEnoughData": true,
  "scores": { ... }, "weakestDirection": "Vocabulary", "mistakes": { ... },
  "weaknesses": [ { "text": "...", "count": 3 } ],
  "recommendations": [ { "text": "...", "count": 3 } ],
  "groups": [
    { "groupId": 3, "name": "11-A", "studentCount": 3, "essayCount": 5, "averageTotal": 2.84 },
    { "groupId": 4, "name": "9-B",  "studentCount": 1, "essayCount": 1, "averageTotal": 4.2 }
  ]
}
```

- `essayCount` — müəllimin **bütün** esseləri (şagird seçilməyənlər də daxil), `essaysWithStudent` — onlardan neçəsi bir şagirdə bağlıdır. Fərq = "özüm üçün yoxladıqlarım".
- `groups[]` — hər qrupun qısa sətri; `averageTotal: null` = həmin qrupda hələ esse yoxdur. Bura klik → §6.3 ekranı.

### 6.5. Təklif olunan analitika UI axını

1. "Şagirdlərim" bölməsinin yuxarısında **ümumi panel** kartları: ümumi esse sayı, orta bal (`scores.total` + `totalPercent`), ən zəif istiqamət, son 30 gündəki esse sayı.
2. Qrup siyahısında hər sətirdə `averageTotal` göstərin → klik → **qrup ekranı**: 4 istiqamətli bar (`percent`), səhv pastası, şagird sıralaması.
3. Şagird sətrinə klik → **şagird profili**: trend qrafiki, `delta` göstəricisi, səhv profili, "Zəif tərəflər" və "Tövsiyələr" siyahıları.
4. Hər üç ekranda `hasEnoughData === false` halını ayrıca işləyin — boş qrafik göstərməkdənsə izahlı boş vəziyyət daha yaxşıdır.

---

## 7. Hələ YOXDUR (gələcək iterasiya)

- **Alt-kateqoriya səviyyəsində zəiflik** (məs. "artikllərdə 12 səhv", "sözönlərində 8 səhv") — hazırda səhvlər yalnız 4 kateqoriyaya bölünür (`Grammar` / `Spelling` / `Vocabulary` / `NaturalExpression`). Daha dərin bölgü AI promptunda və bazada dəyişiklik tələb edir və yalnız **yeni** esselərə şamil olunacaq.
- **AI-ın yazdığı ümumi hesabat mətni** ("bu şagird üçün 3 aylıq plan") — hazırda tövsiyələr esse rəylərindən götürülür, ayrıca yazılmır.

---

## 8. Yekun yoxlama siyahısı (frontend üçün)

- [ ] "Şagirdlərim" bölməsi: qrup CRUD + şagird CRUD (`/api/groups`, `/api/students`)
- [ ] Esse formasında opsional şagird seçici (`studentId`), seçiləndə sinif avtomatik doldurulur/gizlədilir
- [ ] `grade` sahəsi artıq həmişə göndərilmək məcburi deyil — yalnız şagirdin sinfi yoxdursa məcburi göstərin
- [ ] `400 "Şagird tapılmadı."` xətası uyğun mesajla göstərilir (nadir hal — UI-da şagird artıq silinibsə baş verə bilər)
- [ ] Tarixçədə `studentName` göstərilir, `studentId`/`groupId` ilə filtr (opsional)
- [ ] Silmə əməliyyatlarında "esse tarixçəsi qalır" izahı
- [ ] Qrup/şagird limiti aşılanda (50 qrup / 200 şagird) gələn `400` mesajı göstərilir
- [ ] Analitika ekranları: `/api/analytics/overview`, `/groups/{id}`, `/students/{id}`
- [ ] İstiqamət diaqramlarında `percent` istifadə olunur, `average` yox (maksimumlar fərqlidir!)
- [ ] Şagird müqayisəsində `mistakes.perHundredWords` istifadə olunur, xam `total` yox
- [ ] `hasEnoughData === false` halında trend qrafiki əvəzinə izahlı boş vəziyyət göstərilir
- [ ] `rank: 0` olan şagirdlər "Hələ esse yoxdur" kimi göstərilir
