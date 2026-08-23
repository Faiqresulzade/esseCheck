# EssayCheck AI — Qeydiyyatda 1 Aylıq Pulsuz Pro (Cihaza Bağlı)

Yeni qeydiyyatdan keçən istifadəçi **avtomatik 1 aylıq Pro** alır. Sui-istifadənin qarşısını almaq
üçün bu, hesaba yox, **cihaza** bağlıdır: bir telefon ömrü boyu yalnız bir dəfə pulsuz ay ala bilər.

**Bu, breaking dəyişiklik deyil** — `RegisterRequest`-ə yalnız **opsional** sahələr əlavə olunub.
Köhnə tətbiq versiyası işləməyə davam edir, sadəcə trial almır (§3).

---

## 1. Frontend nə etməlidir

`POST /api/auth/register` sorğusuna **iki yeni sahə** əlavə edin:

```json
{
  "fullName": "Aygün Məmmədova",
  "email": "aygun@example.com",
  "password": "Test1234",
  "confirmPassword": "Test1234",
  "acceptTerms": true,

  "deviceId": "a1b2c3d4e5f6a7b8",
  "integrityToken": null
}
```

### `deviceId` — **MƏCBURİ ETMƏLİSİNİZ** (backend üçün opsionaldır)

Android-də `Settings.Secure.ANDROID_ID`:

```kotlin
val deviceId = Settings.Secure.getString(contentResolver, Settings.Secure.ANDROID_ID)
```

- Tətbiq silinib yenidən qurulanda **dəyişmir** (bizə lazım olan xassə budur).
- Factory reset-də dəyişir — bunu qəbul edirik.
- **Göndərilməsə istifadəçi trial ALMIR** (Free planda qalır). Yəni bu sahəni unutmaq
  istifadəçini pulsuz aydan məhrum edir — sınaqda mütləq yoxlayın.

### `integrityToken` — hələlik `null` göndərin

Google Play Integrity **hələ qurulmayıb**. Sahə indidən qəbul olunur ki, sonra server tərəfdə
yoxlama aktivləşəndə **tətbiqin yeni versiyası tələb olunmasın**. Play Integrity quraşdırıldıqdan
sonra sizə ayrıca deyəcəyik — o vaxt bu sahəyə real token yazmalı olacaqsınız.

---

## 2. Nəticəni necə görürsünüz

Qeydiyyatdan sonra `GET /api/subscription` (mövcud endpoint) cavabı:

**Trial verilib:**
```json
{
  "plan": "Pro",
  "isActive": true,
  "startDate": "2026-08-23T08:46:09.47Z",
  "endDate": "2026-09-22T08:46:09.47Z",
  "autoRenew": false,
  "platform": "Trial"
}
```

**Trial verilməyib (cihaz artıq istifadə edib, ya da `deviceId` göndərilməyib):**
```json
{ "plan": "Free", "isActive": true, "startDate": null, "endDate": null, "autoRenew": false, "platform": null }
```

`"platform": "Trial"` — **yeni enum dəyəri**. Real satınalmadan (`"GooglePlay"`) ayırmaq üçündür.
Əgər `platform` sahəsinə görə switch/when yazmısınızsa, bu dəyəri əlavə edin.

> **Qeydiyyat cavabının özü dəyişməyib.** `POST /api/auth/register` yenə sadəcə
> `{ "succeeded": true, "message": "Qeydiyyat uğurla tamamlandı." }` qaytarır — trial alınıb-alınmadığını
> **demir**. Bunu bilmək üçün login-dən sonra `GET /api/subscription` çağırın.

---

## 3. Davranış cədvəli

| Hal | Nəticə |
|---|---|
| Yeni cihaz + `deviceId` göndərilib | ✅ 1 ay Pro (`platform: "Trial"`) |
| **Eyni cihazda ikinci hesab** | ❌ Free — qorumanın əsas məqsədi budur |
| Hesabı silib yenidən qeydiyyat (eyni cihaz) | ❌ Free — cihaz qeydi hesabla birlikdə silinmir |
| `deviceId` göndərilməyib | ❌ Free |
| Fərqli cihaz | ✅ 1 ay Pro |

Hər dörd ssenari canlı sistemdə test olunub.

**Vacib:** trial verilmədikdə qeydiyyat **uğursuz olmur** — istifadəçi normal qeydiyyatdan keçir,
sadəcə Free planda başlayır. Heç bir xəta mesajı göstərməyin.

---

## 4. UX tövsiyəsi — istifadəçini çaşdırmayın

İkinci hesab açan istifadəçi "niyə mənə pulsuz ay verilmədi?" deyə bilər. Tövsiyə:

- Qeydiyyat ekranında "**Yeni istifadəçilər üçün 1 ay pulsuz Pro**" yazın, amma yanında kiçik
  şriftlə "*hər cihaz üçün bir dəfə*" qeyd edin.
- Qeydiyyatdan sonra `GET /api/subscription` cavabına baxıb:
  - `platform === "Trial"` → "1 aylıq Pro hədiyyəniz aktivdir! 🎉"
  - `plan === "Free"` → sadəcə adi Free ekranı göstərin, "trial almadınız" kimi mənfi mesaj **yazmayın**.

---

## 5. Bilməli olduğunuz məhdudiyyət (dürüst qeyd)

Hazırkı qoruma **adi istifadəçini** saxlayır, **qəsdli/texniki bilikli şəxsi yox**:

- ANDROID_ID root edilmiş cihazda və ya emulyatorda dəyişdirilə bilər.
- Factory reset ANDROID_ID-ni sıfırlayır → yeni trial mümkün olur.
- API-yə birbaşa (tətbiqdən kənar) sorğu göndərən şəxs istənilən `deviceId` uydura bilər.

Bu boşluqlar **Play Integrity qurulduqdan sonra bağlanacaq** — token cihaz ID-sinin həqiqi
tətbiqdən, həqiqi cihazdan gəldiyini sübut edəcək. O vaxta qədər bu, məlum və qəbul edilmiş riskdir.

---

## 6. Mövcud istifadəçilər

Sistemdəki 7 mövcud hesabın hamısına **əl ilə 1 aylıq Pro verildi** (2026-08-23). Onlar üçün
əlavə heç nə etmək lazım deyil.

---

## 7. Yoxlama siyahısı (frontend üçün)

- [ ] `register` sorğusuna `deviceId` (ANDROID_ID) əlavə olunub və **həmişə** göndərilir
- [ ] `integrityToken` sahəsi sorğuda var (hələlik `null`)
- [ ] Qeydiyyatdan sonra `GET /api/subscription` çağırılır, `platform: "Trial"` halı işlənir
- [ ] `platform` enum-una `"Trial"` dəyəri əlavə olunub (switch/when varsa)
- [ ] Trial alınmayanda **xəta göstərilmir**, sadəcə Free ekranı açılır
- [ ] Qeydiyyat ekranında "hər cihaz üçün bir dəfə" qeydi var
