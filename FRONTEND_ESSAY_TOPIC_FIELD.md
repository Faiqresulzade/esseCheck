# EssayCheck AI — Opsional Tapşırıq Mövzusu (`topic`) Sahəsi

Bu sənəd `POST /api/Essay/evaluate` sorğusuna əlavə olunan **opsional** bir sahəni əhatə edir: `topic`. `grade` sahəsindən fərqli olaraq bu **məcburi deyil** — heç nə göndərməsən, sistem əvvəlki kimi işləməyə davam edir.

---

## 1. Nə üçün əlavə olunub

İndiyə qədər AI content (məzmun) balını yalnız **essenin özündən** çıxardığı mövzuya görə qiymətləndirirdi. Amma real DİM imtahanında tələbəyə konkret bir tapşırıq mövzusu verilir (məs. *"Should students wear school uniforms?"*) və əgər tələbə bu mövzudan tam kənara çıxıbsa, bu, real imtahanda balı aşağı salır.

`topic` sahəsi göndərilərsə, AI content balını **məhz bu mövzuya uyğunluğa görə** qiymətləndirir — essenin özündən mövzu çıxarmaqla kifayətlənmir.

---

## 2. Nə vaxt göndərilməlidir

- Əgər tətbiqdə tələbəyə əvvəlcədən müəyyən mövzular təklif olunursa (məs. mövzu siyahısından seçim) — seçilən mövzunu bu sahədə göndər.
- Əgər tələbə sərbəst mövzuda yazırsa (mövzu seçimi yoxdur) — sahəni ötürmə və ya boş göndər, AI mövzunu essenin özündən çıxaracaq (əvvəlki davranış dəyişmir).

**Bu sahə frontend-də UI dəyişikliyi tələb etmir** — mövcud "mövzu seçimi" funksionallığı yoxdursa, heç nə etməyə ehtiyac yoxdur, sistem sən onu əlavə edənə qədər köhnə kimi işləyəcək.

---

## 3. Sorğu formatı

**`POST /api/Essay/evaluate`:**

```json
{
  "text": "Nowadays, many students spend a lot of time using smartphones...",
  "title": "Texnologiya haqqında",
  "source": "Text",
  "grade": "Grade11",
  "topic": "The impact of technology on students' education"
}
```

| Sahə | Tip | Məcburidirmi | Qeyd |
|---|---|---|---|
| `text`, `title`, `source`, `grade` | — | Dəyişməyib | Əvvəlki sənədə bax (`FRONTEND_ESSAY_GRADE_LEVEL.md`) |
| **`topic`** | string? | **Xeyr — opsional** | Maks. 300 simvol. Göndərilməsə və ya boş/`null` olsa, AI mövzunu essenin özündən çıxarır |

**`topic` göndərilmədən (əvvəlki kimi):**
```json
{
  "text": "Nowadays, many students spend a lot of time using smartphones...",
  "grade": "Grade11"
}
```
Bu, tam etibarlıdır və indiyə qədər olduğu kimi işləyir.

---

## 4. Nəyə təsir edir

- Yalnız **`scores.content`** balına təsir edir. Digər ballar (structure, grammar, vocabulary) və bütün digər sahələr eyni qalır.
- Əgər esse `topic`-dən tamamilə kənardırsa, `content` balı **0** ola bilər — bu, xəta deyil, düzgün DİM qiymətləndirməsidir.
- Cavabda (`response`) `topic` üçün ayrıca bir sahə **yoxdur** — sorğuda göndərdiyin mövzu tarixçədə/detalda geri qaytarılmır (hazırda saxlanılmır, yalnız qiymətləndirmə anında istifadə olunur).

---

## 5. Xəta halları

`topic` 300 simvoldan uzun göndərilsə → HTTP 400 (real test edilib):
```json
{
  "succeeded": false,
  "message": "The field Topic must be a string or array type with a maximum length of '300'.",
  "errors": ["The field Topic must be a string or array type with a maximum length of '300'."]
}
```
(Bu mesaj ingiliscədir — sadəcə uzunluq validasiyasıdır, digər sahələrdəki kimi Azərbaycanca xüsusi mesaj təyin edilməyib, çünki adətən UI özü mətn sahəsinin uzunluğunu məhdudlaşdıracaq.)

Başqa xüsusi validasiya yoxdur — boş sətir (`""`), `null` və sahənin ümumiyyətlə olmaması eyni şəkildə (mövzu AI tərəfindən essedən çıxarılır) qəbul edilir.

---

## 6. Yekun

- [ ] Əgər tətbiqdə mövzu seçimi/tapşırıq mətni funksionallığı varsa, `POST /api/Essay/evaluate` sorğusuna `topic: "..."` əlavə et
- [ ] Yoxdursa, heç nə etmə — sistem geriyə uyğundur (backward compatible)
