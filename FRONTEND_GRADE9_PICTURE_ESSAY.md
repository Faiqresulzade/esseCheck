# EssayCheck AI — 9-cu Sinif: Şəkil-əsaslı Esse (DİM Formatı)

Bu sənəd 9-cu sinif üçün **tam yeni bir axını** əhatə edir: real DİM imtahanında 9-cu sinif şagirdinə 3 şəkil verilir və o, essesini bu şəkillər əsasında yazır. Bu, 9-cu sinifdə **köhnə sərbəst-mövzu rejimini tamamilə əvəz edir** — artıq 9-cu sinif üçün başqa yol yoxdur.

Backend tərəfi tam hazırdır, real şəkillərlə test edilib (aşağıda nəticələr var).

---

## 1. Əsas dəyişiklik — köhnə endpoint artıq Grade9 qəbul etmir

**`POST /api/Essay/evaluate`** (mövcud, mətn-əsaslı endpoint) indi **Grade9 göndərilsə rədd edir**:

```json
// Sorğu: { "text": "...", "grade": "Grade9" }
// Cavab: HTTP 400
{
  "message": "9-cu sinif üçün esse yalnız 3 promt-şəkli ilə göndərilməlidir: /api/Essay/evaluate/grade9-images."
}
```

**Nəticə:** frontend-də 9-cu sinif seçiləndə, istifadəçi artıq "sərbəst mövzu" ekranına deyil, birbaşa **"3 şəkil yüklə"** ekranına yönləndirilməlidir. `POST /api/Essay/evaluate` yalnız **11-ci sinif** üçün istifadə olunmağa davam edir (heç nə dəyişməyib).

---

## 2. Yeni endpoint

```
POST /api/Essay/evaluate/grade9-images
Content-Type: multipart/form-data
```

| Sahə | Tip | Məcburidirmi | Qeyd |
|---|---|---|---|
| `text` | string (form field) | Bəli | Şagirdin yazdığı esse mətni (max 5000 simvol) |
| `title` | string (form field) | Xeyr | Boş qalsa avtomatik yaradılır |
| `promptImage1` | fayl (şəkil) | **Bəli** | DİM-in verdiyi 1-ci şəkil |
| `promptImage2` | fayl (şəkil) | **Bəli** | 2-ci şəkil |
| `promptImage3` | fayl (şəkil) | **Bəli** | 3-cü şəkil |

**`grade` sahəsi göndərilmir** — bu endpoint həmişə Grade9 kimi qiymətləndirir, çünki yalnız bunun üçün mövcuddur.

Maksimum ümumi sorğu ölçüsü: **15 MB** (3 şəkil + mətn birlikdə).

---

## 3. Plan məhdudiyyəti — VACİB

Bu funksiya **yalnız Pro Plus** planında mövcuddur (eynilə hazırkı OCR/"Şəkildən oxu" funksiyası kimi — eyni gündəlik limit mexanizmini paylaşır).

**Nəticə:** Free və Pro planındakı 9-cu sinif istifadəçiləri **heç bir essesini yoxlaya bilməyəcək**, çünki 9-cu sinif üçün başqa yol qalmayıb. Free/Pro istifadəçi bu endpoint-ə sorğu göndərsə:

```json
// HTTP 403
{ "message": "Şəkildən esse oxuma yalnız Pro Plus üçün əlçatandır." }
```

Bu, hazırkı **plan/qiymət siyasətinizə birbaşa təsir edir** — 9-cu sinif şagirdlərinin tətbiqi ödənişsiz sınaya bilməyəcəyini nəzərə alın (bu, məhsul qərarıdır, backend-in özü yalnız mövcud OCR qaydasını təkrarlayır).

---

## 4. Uğurlu cavab

Format **tamamilə eyni** qalır (11-ci sinif cavabı ilə eyni struktur) — sadəcə `grade: "Grade9"`, `source: "Text"` gəlir:

```json
{
  "id": 54,
  "title": "...",
  "grade": "Grade9",
  "source": "Text",
  "wordCount": 61,
  "scores": {
    "structure": 1,
    "content": 2,
    "contentComment": "Mətn şəkillərdəki mövzunu tam əhatə edir, hadisələr aydın şəkildə izah olunur.",
    "grammar": 0.8,
    "vocabulary": 0.9,
    "total": 4.7
  },
  "mistakes": [ "..." ],
  "feedback": { "..." : "..." }
}
```

**`content` balı artıq mövzu mətninə görə deyil, AI-ın şəkillərdə HƏQİQƏTƏN gördüyünə görə verilir.** Bunu real test etdim:

| Test | Nəticə |
|---|---|
| Esse şəkillərdəki əhvalatı düzgün təsvir edir | `content: 2` (maksimum), şərh: "...şəkillərdəki mövzunu tam əhatə edir" |
| Esse tamam başqa mövzudadır (məs. futbol haqqında) | `content: 0`, şərh: "...şəkillərlə əlaqəli deyil" |

---

## 5. Xəta halları

**Şəkillərdən biri əskikdirsə** → HTTP 400:
```json
{ "succeeded": false, "message": "The promptImage3 field is required.", "errors": ["..."] }
```

**Fayl boşdur/göndərilməyib, amma sahə mövcuddur:**
```json
{ "message": "3 promt-şəkli də tələb olunur." }
```

**Şəkil olmayan fayl (məs. PDF) göndərilsə:**
```json
{ "message": "Yalnız şəkil faylları qəbul olunur." }
```

**Mətn boşdursa:**
```json
{ "message": "Esse mətni boş ola bilməz." }
```

---

## 6. UI axını təklifi

1. İstifadəçi 9-cu sinfi seçir → "Sərbəst mövzu" seçimi göstərilmir, birbaşa **"3 şəkil yüklə"** ekranı açılır.
2. İstifadəçi 3 şəkli kameradan çəkir və ya qalereyadan seçir (DİM imtahan vərəqəsindəki şəkillər).
3. Bu 3 şəkil aşağıda, kiçik önizləmə kimi göstərilir (istəyərsə dəyişə bilsin).
4. İstifadəçi essesini yazır (mövcud mətn sahəsi, dəyişmədən istifadə oluna bilər).
5. "Yoxla" düyməsi bütün 4 hissəni (`text` + 3 şəkil) `multipart/form-data` kimi `/api/Essay/evaluate/grade9-images`-ə göndərir.
6. Cavab **tamamilə eyni ekranda** göstərilir (11-ci sinif nəticə ekranı ilə eyni komponent istifadə oluna bilər — format identikdir).

**Diqqət:** göndərilən 3 şəkil backend-də **saxlanılmır** (yalnız AI çağırışı üçün istifadə olunur, DB-yə yazılmır) — tarixçədə yalnız yazılan esse mətni və nəticə görünür, şəkillərin özü deyil.

---

## 7. Yekun yoxlama siyahısı (frontend üçün)

- [ ] 9-cu sinif seçimi artıq "sərbəst mövzu" ekranına deyil, "3 şəkil yüklə" ekranına aparır
- [ ] Kamera/qalereyadan 3 ayrı şəkil seçmə UI-ı əlavə olundu
- [ ] Sorğu `multipart/form-data` formatında, `promptImage1/2/3` + `text` (+ opsional `title`) sahələri ilə göndərilir
- [ ] Free/Pro istifadəçi üçün 403 cavabı düzgün göstərilir ("Pro Plus tələb olunur" mesajı)
- [ ] Şəkil əskikdirsə frontend-side yoxlama (backend-ə getməzdən əvvəl bütün 3 şəkil seçilib mi)
- [ ] Nəticə ekranı dəyişməyib — mövcud komponent istifadə oluna bilər
