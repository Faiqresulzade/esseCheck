# EssayCheck AI — "Təbii ifadə" (NaturalExpression) Statistika Kartı Əskikdir

Bu sənəd Tarixçə detalı ekranındakı "Səhvlərin statistikası" bölməsində tapılan bir uyğunsuzluğu izah edir: backend 4 kateqoriya qaytarır, ekranda isə yalnız 3 kart göstərilir.

---

## 1. Problem

`GET /api/Essay/history/{id}` (və qiymətləndirmə cavabı) `statistics` obyektində **4** sahə qaytarır:

```json
{
  "statistics": {
    "grammar": 0,
    "spelling": 0,
    "vocabulary": 1,
    "naturalExpression": 1,
    "total": 2
  }
}
```

Ekranda isə yalnız 3 kart var: **Qrammatika**, **Orfoqrafiya**, **Leksik səhvlər**. `naturalExpression` üçün ayrıca kart yoxdur.

Nəticədə: "Ümumi" (total) sayı 2 göstərir, amma görünən 3 kartın cəmi (0+0+1) yalnız 1-ə bərabərdir — istifadəçiyə ədədlər səhv görünür, halbuki backend riyaziyyatı tam düzgündür (`total = grammar + spelling + vocabulary + naturalExpression`).

Səhvlər siyahısında bu kateqoriya artıq görünür — "Tabii ifadə" etiketi ilə (məs. "really mindful → mindful", "The word 'really' is unnecessary..."). Yəni məlumat mövcuddur, sadəcə yuxarıdakı statistika kartları bölməsində əksini tapmır.

---

## 2. Həll

4-cü stat kartı əlavə edin: **"Təbii ifadə"** (və ya "Üslub"), `statistics.naturalExpression` dəyərini göstərsin.

Tövsiyə olunan yerləşdirmə: mövcud 2x2 grid-i 2x2-dən dəyişməyə ehtiyac yoxdur, sadəcə 4-cü kartı əlavə edin (məs. "Ümumi" kartının yanına, ya da grid-i 2x2-dən başqa formaya salmadan 4 kateqoriya kartını üst-üstə, "Ümumi"ni isə ayrıca/aşağıda saxlaya bilərsiniz — dizayn qərarı sizindir).

Rəng tövsiyəsi: səhvlər siyahısındakı "Tabii ifadə" etiketi ilə eyni rəng sxemini (yaşılımtıl) istifadə edin ki, kart və etiket vizual olaraq əlaqələndirilsin.

---

## 3. Sahə adları (API cavabı)

| JSON sahəsi | Azərbaycanca etiket (mövcud konvensiya) |
|---|---|
| `statistics.grammar` | Qrammatika |
| `statistics.spelling` | Orfoqrafiya |
| `statistics.vocabulary` | Leksik səhvlər |
| `statistics.naturalExpression` | **Təbii ifadə** (əskik olan) |
| `statistics.total` | Ümumi |

`mistakes` massivindəki hər elementin `category` sahəsi də eyni 4 dəyərdən birini alır: `"Grammar"`, `"Spelling"`, `"Vocabulary"`, `"NaturalExpression"` — səhvlər siyahısındakı "Tabii ifadə" etiketi artıq bunu düzgün göstərir, dəyişikliyə ehtiyac yoxdur.

---

## 4. Yekun yoxlama siyahısı (frontend üçün)

- [ ] Tarixçə detalı ekranında 4-cü stat kartı ("Təbii ifadə") əlavə olunub
- [ ] Kart `statistics.naturalExpression` dəyərini göstərir
- [ ] Görünən 4 kartın cəmi (Qrammatika + Orfoqrafiya + Leksik + Təbii ifadə) həmişə "Ümumi" ilə üst-üstə düşür
- [ ] `naturalExpression: 0` olduqda kart yenə göstərilir (0 dəyəri ilə), gizlədilmir — digər kateqoriyalarla eyni davranış
