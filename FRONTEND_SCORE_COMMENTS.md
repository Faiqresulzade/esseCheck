# EssayCheck AI — Meyar Şərhləri (`*Comment`) və Dəqiq Bal Dəyişikliyi

Bu sənəd `POST /api/Essay/evaluate` cavabında (və `GET /api/Essay/history/{id}` detalında) `scores` obyektinə edilən dəyişikliyi əhatə edir. **Sorğu formatına heç bir təsiri yoxdur** — yalnız cavab (response) formatı dəyişib.

---

## 1. Nə üçün dəyişdi

Tarixçə detalı ekranında "Meyar | Bal | Şərh" cədvəli var idi, amma **Şərh sütunu həmişə boş idi** — backend heç bir izah göndərmirdi. İndi hər meyar üçün AI-ın həmin balı **niyə** verdiyini izah edən mətn əlavə olunub.

Əlavə olaraq bal dəqiqliyi **0.5 addımdan 0.1 addıma** keçirildi (aşağıda ətraflı) və Güclü/Zəif/Tövsiyə bölmələri xeyli detallandırıldı.

---

## 2. `scores` obyektinin yeni forması

**Əvvəl:**
```json
"scores": {
  "structure": 1.0,
  "content": 2.0,
  "grammar": 0.5,
  "vocabulary": 1.0,
  "total": 4.5
}
```

**İndi (hər balın yanında `*Comment` sahəsi):**
```json
"scores": {
  "structure": 0.5,
  "structureComment": "Məqalədə giriş, əsas hissə və nəticə aydın görünür, lakin əsas hissə çox qısa olduğu üçün ideyalar tam inkişaf etdirilməyib. Bu səbəbdən struktur tam bal ala bilməyib.",
  "content": 1.0,
  "contentComment": "Mövzu ilə bağlı əsas ideyalar qeyd olunub, lakin onlar nümunələr və səbəblər olmadan sadəcə siyahı şəklində verilib. Məqalə 100 sözdən az olduğu üçün məzmun inkişaf etdirilməyib.",
  "grammar": 1.0,
  "grammarComment": "Məqalədə qramatik səhv yoxdur. Cümlələr düzgün qurulub və bağlayıcı sözlər (However, In conclusion) düzgün istifadə olunub.",
  "vocabulary": 1.0,
  "vocabularyComment": "Lüğət mövzuya uyğundur və dəqiq istifadə olunub (smartphones, communicate, entertainment, harmful). Sözlər müxtəlif və zəngindir.",
  "total": 3.5
}
```

(Bu, real backend cavabından götürülmüş sınaqdan keçirilmiş nümunədir.)

### Sahə cədvəli

| Sahə | Tip | Qeyd |
|---|---|---|
| `structure` | number | Dəyişməyib, amma indi 0.1 addımla (aşağıya bax) |
| **`structureComment`** | string | **Yeni.** Həmişə doludur (boş sətir gəlməməlidir) |
| `content` | number | Dəyişməyib, 0.1 addımla |
| **`contentComment`** | string | **Yeni** |
| `grammar` | number | Dəyişməyib, 0.1 addımla |
| **`grammarComment`** | string | **Yeni** |
| `vocabulary` | number | Dəyişməyib, 0.1 addımla |
| **`vocabularyComment`** | string | **Yeni** |
| `total` | number | Dəyişməyib (4 balın cəmi, maks. 5) |

**Bütün mətn sahələri (comment-lər) Azərbaycancadır.**

---

## 3. UI dəyişikliyi — "Şərh" sütunu

Ekrandakı "DİM tərzli qiymətləndirmə" cədvəlində boş qalan **Şərh** sütununu indi doldurmaq olar:

| Meyar | Bal | Şərh |
|---|---|---|
| Mövzu və struktur (0-1) | `scores.structure` | `scores.structureComment` |
| Mövzunun əhatə olunması (0-2) | `scores.content` | `scores.contentComment` |
| Qrammatika və dil istifadəsi (0-1) | `scores.grammar` | `scores.grammarComment` |
| Leksik ehtiyat (0-1) | `scores.vocabulary` | `scores.vocabularyComment` |

Şərh mətnləri bəzən 1-2 cümlə ola bilər (uzunluğa görə UI-da mətnin sətir sınması/genişlənməsi nəzərə alınmalıdır — sabit hündürlükdə kəsilməsin).

---

## 4. Bal dəqiqliyi: 0.5 → 0.1 addım

**Əvvəl:** ballar yalnız 0, 0.5, 1 (structure/grammar/vocabulary) və ya 0, 0.5, 1, 1.5, 2 (content) kimi dəyərlər ala bilirdi.

**İndi:** ballar **0.1 addımla** istənilən dəyəri ala bilər — məsələn `0.3`, `0.7`, `0.9`. Maksimum dəyərlər dəyişməyib (structure/grammar/vocabulary hələ də maks. 1, content maks. 2, total maks. 5).

**Frontend üçün təsiri:**
- Əgər UI-da bal göstərilməsi (məs. proqres zolağı, dəyirmi göstərici) əvvəllər yalnız `0`, `0.5`, `1.0` kimi diskret mövqelər üçün nəzərdə tutulubsa, indi istənilən ondalıq dəyəri düzgün göstərməlidir.
- `total` və `accuracyPercent` sahələrinin format/dəyirmiləşdirmə məntiqi dəyişməyib — sadəcə daha dəqiq dəyərlər gələ bilər (məs. `total: 3.4` əvəzinə həmişə `.0`/`.5` ilə bitən dəyər).

---

## 5. Güclü/Zəif/Tövsiyə hissələri — daha detallı

`feedback.strengths`, `feedback.weaknesses`, `feedback.recommendations` massivlərinin **strukturu dəyişməyib** (hələ də sətir massivi), amma məzmun keyfiyyəti dəyişib:

| | Əvvəl | İndi |
|---|---|---|
| Element sayı | 1-3 qısa maddə | 3-5 detallı maddə (esse çox qısadırsa daha az ola bilər) |
| Məzmun | Ümumi ("Yaxşı struktur qurmusan") | Konkret, essedən sitat gətirən ("'However' kimi keçid sözlərindən düzgün istifadə etmisiniz...") |

**Frontend üçün təsiri:** əgər UI-da bu siyahılar üçün sabit hündürlük/maksimum sətir sayı təyin edilibsə, indi daha uzun mətnlər üçün skroll və ya genişlənən kart lazım ola bilər. Element sayı da artdığı üçün (3-5) siyahının vizual yeri bir qədər daha çox yer tuta bilər.

---

## 6. Yekun yoxlama siyahısı (frontend üçün)

- [ ] "DİM tərzli qiymətləndirmə" cədvəlindəki **Şərh** sütununu `scores.structureComment` / `contentComment` / `grammarComment` / `vocabularyComment` ilə doldur
- [ ] Bal göstəricilərinin (proqres zolağı və s.) 0.1 addımlı ondalıq dəyərləri düzgün render etdiyini yoxla
- [ ] Güclü/Zəif/Tövsiyə kartlarının 3-5 (əvvəlki 1-3 əvəzinə) və daha uzun mətnli elementləri düzgün göstərdiyini yoxla (uzun mətn üçün UI daşmasın)
