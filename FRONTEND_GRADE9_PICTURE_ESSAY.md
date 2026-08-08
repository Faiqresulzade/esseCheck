# EssayCheck AI — 9-cu Sinif: Şəkil-əsaslı Esse (DİM Formatı)

Bu sənəd 9-cu sinif üçün **tam yeni bir axını** əhatə edir: real DİM imtahanında 9-cu sinif şagirdinə şəkillər verilir və o, essesini bu şəkillər əsasında yazır. Bu, 9-cu sinifdə **köhnə sərbəst-mövzu rejimini tamamilə əvəz edir** — artıq 9-cu sinif üçün başqa yol yoxdur.

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

**Nəticə:** frontend-də 9-cu sinif seçiləndə, istifadəçi artıq "sərbəst mövzu" ekranına deyil, birbaşa **"şəkil yüklə"** ekranına yönləndirilməlidir. `POST /api/Essay/evaluate` yalnız **11-ci sinif** üçün istifadə olunmağa davam edir (heç nə dəyişməyib).

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
| `promptImage1` | fayl (şəkil) | **Xeyr — opsional** | DİM-in verdiyi 1-ci şəkil |
| `promptImage2` | fayl (şəkil) | **Xeyr — opsional** | 2-ci şəkil |
| `promptImage3` | fayl (şəkil) | **Xeyr — opsional** | 3-cü şəkil |

**`grade` sahəsi göndərilmir** — bu endpoint həmişə Grade9 kimi qiymətləndirir, çünki yalnız bunun üçün mövcuddur.

### Şəkillər tam opsionaldır — 0, 1, 2 və ya 3 göndərmək olar

- **0 şəkil** — istifadəçi essesini şəkilsiz göndərə bilər. Bu halda AI mövzunu essenin özündən çıxarır (11-ci sinifdəki "mövzu göndərilməyəndə" davranışı ilə eyni). **Bu halda Pro Plus tələb OLUNMUR** — hər plan istifadə edə bilər (aşağıya bax).
- **1 və ya 2 şəkil** — göndərilən şəkillər əsasında qiymətləndirilir, göndərilməyənlər sadəcə nəzərə alınmır.
- **3 şəkil** — tam DİM formatı, ən dəqiq qiymətləndirmə.

Göndərilməyən sahələr formda ümumiyyətlə olmamalıdır (boş fayl yox, sahənin özü göndərilməməlidir).

Maksimum ümumi sorğu ölçüsü: **15 MB**.

---

## 3. Plan məhdudiyyəti — YALNIZ ŞƏKİL GÖNDƏRİLƏNDƏ

| Vəziyyət | Tələb olunan | Limit |
|---|---|---|
| **0 şəkil** (yalnız mətn) | Hər plan (Free daxil) | Adi gündəlik mətn limiti (Free: 1/gün) |
| **1, 2 və ya 3 şəkil** | **Yalnız Pro Plus** | Limitsiz (Pro Plus-da OCR limitsizdir) |

Bu, mövcud "Şəkildən oxu" (OCR) funksiyası ilə **eyni qaydanı** paylaşır — çünki hər ikisi eyni resursu (vision) istifadə edir. Free/Pro istifadəçi şəkil əlavə etməyə cəhd etsə:

```json
// HTTP 403
{ "message": "Şəkildən esse oxuma yalnız Pro Plus üçün əlçatandır." }
```

Amma **eyni istifadəçi şəkilsiz göndərsə**, HTTP 200 alır (adi mətn limiti daxilində).

**Frontend üçün nəticə:** "3 şəkil yüklə" düymələrini Free/Pro istifadəçilərə göstərmə (və ya göstərsən "Pro Plus lazımdır" bildirişi ver), amma **"şəkilsiz göndər" seçimini bütün planlara aç**.

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

**Şəkil(lər) göndəriləndə `content` balı mövzu mətninə görə deyil, AI-ın şəkillərdə HƏQİQƏTƏN gördüyünə görə verilir.** Real test edilib:

| Test | Nəticə |
|---|---|
| Esse şəkillərdəki əhvalatı düzgün təsvir edir (2 və ya 3 şəkil) | `content: 2` (maksimum), şərh: "...şəkillərdəki mövzunu tam əhatə edir" |
| Esse tamam başqa mövzudadır (məs. futbol haqqında) | `content: 0`, şərh: "...şəkillərlə əlaqəli deyil" |
| 0 şəkil göndərilib | Mövzu essenin özündən çıxarılır, adi (11-ci sinif "mövzu yoxdur" rejimi) kimi qiymətləndirilir |

---

## 5. Xəta halları

**Şəkil olmayan fayl (məs. PDF) göndərilsə** (hər hansı bir şəkil sahəsində):
```json
{ "message": "Yalnız şəkil faylları qəbul olunur." }
```

**Mətn boşdursa:**
```json
{ "message": "Esse mətni boş ola bilməz." }
```

**Free/Pro istifadəçi şəkil əlavə etsə:**
```json
// HTTP 403
{ "message": "Şəkildən esse oxuma yalnız Pro Plus üçün əlçatandır." }
```

**Free istifadəçi 0 şəkillə, amma gündəlik limiti bitibsə:**
```json
// HTTP 429
{ "message": "Bugünkü pulsuz limit (1) bitib. Sabah yenilənəcək və ya Pro planına keçin." }
```

---

## 6. UI axını təklifi

1. İstifadəçi 9-cu sinfi seçir → "Sərbəst mövzu" seçimi göstərilmir.
2. Ekranda şəkil yükləmə seçimi göstərilir: **"Şəkil əlavə et (opsional)"** — istifadəçi 0-3 şəkil əlavə edə bilər (kameradan çəkib və ya qalereyadan seçib).
3. Free/Pro istifadəçiyə: "Şəkil əlavə etmək üçün Pro Plus lazımdır" bildirişi göstərilə bilər, amma yenə də şəkilsiz davam edə bilsin.
4. İstifadəçi essesini yazır (mövcud mətn sahəsi, dəyişmədən istifadə oluna bilər).
5. "Yoxla" düyməsi `text` + seçilmiş şəkil(lər)i (yalnız faktiki seçilənləri, boş sahə göndərməyin) `multipart/form-data` kimi `/api/Essay/evaluate/grade9-images`-ə göndərir.
6. Cavab **tamamilə eyni ekranda** göstərilir (11-ci sinif nəticə ekranı ilə eyni komponent istifadə oluna bilər — format identikdir).

**Diqqət:** göndərilən şəkillər backend-də **saxlanılmır** (yalnız AI çağırışı üçün istifadə olunur, DB-yə yazılmır) — tarixçədə yalnız yazılan esse mətni və nəticə görünür, şəkillərin özü deyil.

---

## 7. Yekun yoxlama siyahısı (frontend üçün)

- [ ] 9-cu sinif seçimi artıq "sərbəst mövzu" ekranına deyil, "şəkil əlavə et (opsional)" ekranına aparır
- [ ] Kamera/qalereyadan 0-3 şəkil seçmə UI-ı əlavə olundu (məcburi deyil)
- [ ] Sorğu `multipart/form-data` formatında, yalnız faktiki seçilmiş `promptImage1/2/3` sahələri + `text` (+ opsional `title`) ilə göndərilir
- [ ] Free/Pro istifadəçi şəkil əlavə etməyə cəhd etsə 403 düzgün göstərilir, amma **şəkilsiz davam edə bilir**
- [ ] Nəticə ekranı dəyişməyib — mövcud komponent istifadə oluna bilər
