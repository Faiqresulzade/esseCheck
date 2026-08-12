# EssayCheck AI — Tətbiq Yeniləmə Bildirişi (Popup)

Bu sənəd tətbiqdə "yeni versiya var, yeniləyin" popup-ını qurmaq üçün backend tərəfini əhatə edir. **Bu, tam opsional bildirişdir** — istifadəçi bağlaya bilər, məcburi yeniləmə/bloklama yoxdur.

---

## 1. Endpoint

```
GET /api/App/version-check?currentVersion=1.2.0
```

Autentifikasiya tələb olunmur (`AllowAnonymous`) — tətbiq açılan kimi, login-dən əvvəl belə çağırıla bilər.

| Parametr | Tip | Məcburidirmi | Qeyd |
|---|---|---|---|
| `currentVersion` | query string | **Bəli** | Tətbiqin cari versiyası, məs. `"1.2.0"` (semver formatında: `major.minor.patch`) |

---

## 2. Cavab

```json
{
  "updateAvailable": true,
  "latestVersion": "1.3.0",
  "playStoreUrl": "https://play.google.com/store/apps/details?id=com.essaycheck.ai"
}
```

| Sahə | Tip | Qeyd |
|---|---|---|
| `updateAvailable` | boolean | `true`-dursa popup göstər |
| `latestVersion` | string? | Play Store-dakı ən son versiya (popup mətnində göstərmək üçün, məs. "1.3.0 versiyası mövcuddur") |
| `playStoreUrl` | string? | "Yenilə" düyməsi bu linki açmalıdır |

**`currentVersion` göndərilmədən çağırılsa** → HTTP 400:
```json
{ "message": "currentVersion parametri tələb olunur." }
```

---

## 3. Versiya müqayisəsi necə işləyir

- Sətir kimi deyil, ədədi (semver) müqayisə olunur — `"1.10.0"` düzgün olaraq `"1.9.0"`-dan **böyük** sayılır (sadə sətir müqayisəsində bu səhv nəticə verərdi).
- `currentVersion` formatı `major.minor.patch` (məs. `"1.2.0"`) olmalıdır — .NET-in `System.Version` formatına uyğun.
- Backend-də `LatestVersion` hələ təyin olunmayıbsa (boş) → **həmişə `updateAvailable: false`** qaytarılır.
- Versiya sətri parse oluna bilmirsə (səhv format göndərilibsə) → yenə **təhlükəsiz defolt olaraq `false`** qaytarılır (yalançı xəbərdarlıq olmasın deyə).

---

## 4. Backend tərəfdə versiya necə yenilənir

Bu, **sizin işiniz deyil** — backend komandası yeni versiya Play Store-a çıxanda Render-in Environment bölməsində iki dəyəri yeniləyir:

```
App__LatestVersion = 1.3.0
App__PlayStoreUrl = https://play.google.com/store/apps/details?id=...
```

Kodda dəyişiklik və ya yenidən deploy tələb olunmur — dəyər dərhal aktiv olur.

---

## 5. Tövsiyə olunan UI axını

1. Tətbiq açılanda (splash/əsas səhifə yüklənəndə) `GET /api/App/version-check?currentVersion={tətbiqin öz versiyası}` çağırılır.
2. `updateAvailable: true` gələrsə, bağlana bilən bir popup/dialog göstərilir:
   - Başlıq: məs. "Yeni versiya mövcuddur"
   - Mətn: məs. "EssayCheck AI-nin {latestVersion} versiyası çıxıb. Ən son funksiyalar üçün yeniləyin."
   - **"Yenilə"** düyməsi → `playStoreUrl`-u brauzerdə/Play Store tətbiqində açır (`Linking.openURL(playStoreUrl)` React Native-də)
   - **"Sonra"** və ya bağlama (X) düyməsi → popup bağlanır, istifadəçi tətbiqi normal istifadə edir
3. Hər sessiyada bir dəfə göstərmək kifayətdir (məs. AsyncStorage-də "bu sessiyada göstərildi" flag-ı saxlana bilər ki, hər ekran keçidində təkrar-təkrar açılmasın).

**Diqqət:** cari tətbiqin öz versiya nömrəsini frontend özü bilməlidir (React Native-də `expo-application` / `react-native-device-info` kimi paketlərdən, ya da `app.json`/`package.json`-dakı `version` sahəsindən oxuna bilər) — bunu backend-ə **siz** göndərirsiniz, backend sizə deyil.

---

## 6. Yekun yoxlama siyahısı (frontend üçün)

- [ ] Tətbiq açılanda `GET /api/App/version-check?currentVersion=...` çağırılır (tətbiqin öz versiyası ilə)
- [ ] `updateAvailable: true` olanda bağlana bilən popup göstərilir
- [ ] "Yenilə" düyməsi `playStoreUrl`-u açır
- [ ] "Sonra"/bağlama düyməsi popup-ı sadəcə bağlayır, heç nəyi bloklamır
- [ ] Popup hər ekran keçidində deyil, sessiya başına bir dəfə göstərilir
