# React Native — Google Play Billing İnteqrasiyası (Frontend üçün Addım-addım Bələdçi)

Bu sənəd EssayCheck AI mobil tətbiqinin (React Native) **Google Play Billing SDK**-nı necə inteqrasiya edəcəyini addım-addım izah edir və ən sonda bunu bizim **backend**-ə (`/api/subscription/google/verify`) necə bağlayacağını göstərir.

> Backend tərəfi artıq hazırdır və test edilib (bax `POSTMAN_DOCS.md`, bölmə 5.6–5.7). Bu sənəd yalnız **frontend/client** tərəfini əhatə edir.

---

## 0. Ümumi Axın (əvvəlcə böyük mənzərəyə bax)

```
[React Native app]                [Google Play]                [EssayCheck Backend]
       │                                │                              │
       │ 1. SDK-nı başlat (initConnection)                             │
       ├───────────────────────────────►│                              │
       │ 2. Məhsulları al (getSubscriptions)                           │
       ├───────────────────────────────►│                              │
       │ 3. İstifadəçi "Pro-ya keç" düyməsinə basır                    │
       │ 4. Satınalma pəncərəsi açılır (requestSubscription)           │
       ├───────────────────────────────►│                              │
       │ 5. Google ödənişi emal edir, purchaseToken qaytarır           │
       │◄───────────────────────────────┤                              │
       │ 6. purchaseToken + productId göndərilir                       │
       ├────────────────────────────────┼─────────────────────────────►│
       │                                │  7. Backend Google-a birbaşa │
       │                                │◄─────doğrulama sorğusu───────┤
       │                                ├───────nəticə─────────────────►│
       │ 8. Backend cavabı (yeni plan) │                              │
       │◄───────────────────────────────┼──────────────────────────────┤
       │ 9. finishTransaction (client tərəfdə "bağla")                 │
       ├───────────────────────────────►│                              │
```

**Əsas prinsip:** Client (mobil app) heç vaxt "mən Pro oldum" deyə özü qərar vermir. Client yalnız Google-dan aldığı `purchaseToken`-i backend-ə çatdırır; **yekun qərarı backend, Google Play Developer API ilə birbaşa yoxlayaraq verir.**

---

## 1. Hansı SDK-nı seçmək

React Native üçün ən çox istifadə olunan və aktiv saxlanılan kitabxana: **`react-native-iap`** (Android-də Google Play Billing Library-ni, iOS-da StoreKit-i əhatə edir).

> **Qeyd:** Bizim backend hazırda yalnız **Google Play (Android)** üçün qurulub (`GooglePlaySettings`, `google/verify`, `google/rtdn`). iOS/App Store tərəfi `SubscriptionPlatform.AppStore` enum-unda yer ayrılıb, amma server-side hələ implementasiya olunmayıb. Bu sənəd Android/Google Play axınına fokuslanır.

---

## 2. Quraşdırma

### 2.1. Paketi quraşdır
```bash
npm install react-native-iap
# və ya
yarn add react-native-iap
```

### 2.2. Android tərəfi
`react-native-iap` avtomatik linklənir (React Native 0.60+, autolinking). Əlavə addım:

**`android/app/build.gradle`** — minimum SDK versiyasını yoxla (Billing Library 5+ üçün `minSdkVersion 21`+ tələb olunur):
```gradle
android {
    defaultConfig {
        minSdkVersion 21
    }
}
```

Heç bir əlavə icazə (`permission`) lazım deyil — Billing Library bunu daxili idarə edir.

### 2.3. iOS tərəfi (gələcək üçün, hazırda backend dəstəkləmir)
```bash
cd ios && pod install
```
(Bu addım yalnız gələcəkdə App Store Billing əlavə olunanda lazım olacaq.)

---

## 3. Google Play Console tərəfində hazırlıq (təkrar xatırlatma)

Bunlar artıq sənin tərəfindən edilməli olan addımlardır (backend sənədində də qeyd olunub):

1. Play Console-da tətbiqi yarat, **package name**-i təyin et (məs. `az.essaycheck.app`).
2. **Monetization → Subscriptions** bölməsində subscription məhsulları yarat. Bizim backend-in gözlədiyi ID-lər (`appsettings.json → GooglePlay:Products`):
   - `pro_monthly` → Pro plan (4.99 AZN/ay)
   - `pro_plus_monthly` → Pro Plus plan (9.99 AZN/ay)
   
   > **Vacib:** Product ID-ni Play Console-da nə cür yaratsan, client kodunda **və** backend `appsettings.json`-da **eyni sətir** olmalıdır (böyük/kiçik hərfə həssasdır).
3. **Test hesabları** əlavə et: Play Console → **Setup → License testing** → test edəcək Gmail hesablarını siyahıya əlavə et (bu hesablar real pul xərcləmədən test satınalması edə bilər).
4. Tətbiqi ən azı **Internal Testing** track-ına yüklə (Billing API tam işləməsi üçün tətbiq Play Console-da closed/internal testing track-da olmalıdır — yerli `debug` build-də bəzi hallarda test məhsulları görünməyə bilər).

---

## 4. SDK-nı başlatmaq (App başlayanda)

Tətbiqin kök komponentində (məs. `App.tsx`) və ya bir `BillingProvider` kontekstində:

```typescript
import { useEffect } from 'react';
import {
  initConnection,
  endConnection,
  purchaseUpdatedListener,
  purchaseErrorListener,
} from 'react-native-iap';

useEffect(() => {
  const setup = async () => {
    await initConnection();
  };
  setup();

  // Satınalma nəticəsini dinləyən listener-lər (bax bölmə 6)
  const purchaseUpdateSub = purchaseUpdatedListener(handlePurchaseUpdate);
  const purchaseErrorSub = purchaseErrorListener(handlePurchaseError);

  return () => {
    purchaseUpdateSub.remove();
    purchaseErrorSub.remove();
    endConnection();
  };
}, []);
```

---

## 5. Məhsulları (planları) əldə etmək

"Planlar" ekranı açılanda, Google Play-dən **qiymət və valyuta məlumatını** çəkmək üçün:

```typescript
import { getSubscriptions } from 'react-native-iap';

const PRODUCT_IDS = ['pro_monthly', 'pro_plus_monthly'];

async function loadSubscriptionPlans() {
  const subscriptions = await getSubscriptions({ skus: PRODUCT_IDS });
  // subscriptions[i].productId, .localizedPrice, .title, .description və s.
  return subscriptions;
}
```

> **Tövsiyə:** Planların **adı, xüsusiyyət siyahısı və AZN qiyməti** üçün bizim backend-in `GET /api/subscription/plans` endpoint-ini istifadə et (bax `POSTMAN_DOCS.md` bölmə 5.1) — bu, bizim daxili DİM/plan məntiqimizlə tam uyğundur. Google-dan gələn `localizedPrice`-ı isə yalnız **Google-un rəsmi qiymət göstəricisi** kimi (ödəniş pəncərəsində) əlavə göstərmək üçün istifadə et.

---

## 6. Satınalma prosesini başlatmaq

İstifadəçi "Pro-ya keç" / "Pro Plus-a keç" düyməsinə basdıqda:

```typescript
import { requestSubscription } from 'react-native-iap';

async function subscribeTo(productId: string) {
  try {
    await requestSubscription({ sku: productId });
    // Nəticə handlePurchaseUpdate listener-ində gələcək (aşağıda)
  } catch (err) {
    console.warn('Satınalma başlada bilmədi:', err);
  }
}

// İstifadə:
// <Button onPress={() => subscribeTo('pro_plus_monthly')} />
```

Bu, Google-un **öz native satınalma pəncərəsini** açır (ödəniş üsulu, təsdiq və s. tamamilə Google tərəfindən idarə olunur — bizim kodumuz bunu göstərmir).

---

## 7. Satınalma nəticəsini tutmaq (Listener)

`purchaseUpdatedListener` — uğurlu (və ya pending) hər satınalmada işə düşür:

```typescript
import { finishTransaction, type Purchase } from 'react-native-iap';

async function handlePurchaseUpdate(purchase: Purchase) {
  const { productId, purchaseToken } = purchase;

  if (!purchaseToken) return;

  try {
    // 8-ci addıma keç: backend-ə göndər
    await verifyPurchaseWithBackend(productId, purchaseToken);

    // Backend uğurla təsdiqlədikdən SONRA client tərəfdə əməliyyatı "bağla".
    // Bu addım unudulsa, Google eyni satınalmanı "pending" saxlayıb
    // tətbiq hər açılanda təkrar bu listener-i işə salacaq.
    await finishTransaction({ purchase, isConsumable: false });

    // UI-ı yenilə (aşağıda bölmə 9)
  } catch (error) {
    console.error('Backend təsdiqi uğursuz oldu:', error);
    // finishTransaction ÇAĞIRMA — token hələ "pending" qalsın ki,
    // tətbiq növbəti dəfə açılanda yenidən cəhd edə bilsin.
  }
}

function handlePurchaseError(error: unknown) {
  // İstifadəçi ödənişi ləğv edibsə (user cancelled) burada gələcək — sadəcə logla, xəta göstərmə.
  console.warn('Satınalma xətası və ya ləğvi:', error);
}
```

> **Vacib qayda:** `finishTransaction`-ı **yalnız backend uğurla cavab verdikdən sonra** çağır. Əks halda backend-ə çatmayan (məs. internet kəsilib) satınalmalar itə bilər.

---

## 8. Backend-ə göndərmək (bizim `google/verify` endpoint-i)

Bu, bütün axının **ən kritik nöqtəsidir** — client-in Google-dan aldığı məlumatı bizim autentifikasiyalı API-ə göndərməyi:

```typescript
async function verifyPurchaseWithBackend(productId: string, purchaseToken: string) {
  const accessToken = await getStoredAccessToken(); // AsyncStorage/SecureStore-dan JWT

  const response = await fetch(`${API_BASE_URL}/api/subscription/google/verify`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({ productId, purchaseToken }),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message ?? 'Abunəlik təsdiqlənmədi.');
  }

  return response.json(); // SubscriptionResponse: { plan, isActive, startDate, endDate, autoRenew, platform }
}
```

**Cavab formatı (`SubscriptionResponse`):**
```json
{
  "plan": "ProPlus",
  "isActive": true,
  "startDate": "2026-07-19T12:00:00Z",
  "endDate": "2026-08-19T12:00:00Z",
  "autoRenew": true,
  "platform": "GooglePlay"
}
```

**Mümkün xəta cavabları** (bax `POSTMAN_DOCS.md` bölmə 5.6 — tam siyahı):
| Status | Səbəb | Client-də nə göstərmək |
|---|---|---|
| `400` — "Naməlum Google Play məhsulu" | `productId` backend-də tanınmır | "Texniki xəta, dəstək ilə əlaqə saxlayın" |
| `400` — "Satınalma aktiv deyil" | Google-da satınalma etibarsızdır | "Satınalma təsdiqlənmədi, yenidən cəhd edin" |
| `400` — "Bu satınalma artıq başqa hesaba bağlıdır" | Token başqa istifadəçiyə aiddir | "Bu hesabla satınalma əlaqələndirilə bilmədi" |
| `401` | JWT bitib/etibarsızdır | Refresh token axınını işə sal (bax `POSTMAN_DOCS.md` bölmə 2.3), sonra təkrar cəhd et |
| `503`/`502` | Backend/Google müvəqqəti əlçatmazdır | "Bir azdan yenidən cəhd edin" + avtomatik retry |

---

## 9. UI-ı yeniləmək

Backend uğurla cavab verdikdən sonra tətbiqin **cari plan** vəziyyətini yenilə:

```typescript
async function refreshSubscriptionState() {
  const [subscription, usage] = await Promise.all([
    apiGet('/api/subscription'),       // cari plan (Planlar ekranı, Ayarlar)
    apiGet('/api/subscription/usage'), // gündəlik limit (Əsas səhifə)
  ]);
  // Redux/Zustand/Context state-ini yenilə
}
```

Bunu həm `verifyPurchaseWithBackend` uğurlu olduqda, həm də **tətbiq hər açılanda** (app foreground-a qayıdanda) çağırmaq tövsiyə olunur — çünki abunəlik server tərəfdə **RTDN vasitəsilə arxa planda da** dəyişə bilər (məs. avtomatik yenilənmə, ləğv, ödəniş problemi).

---

## 10. Mövcud satınalmaları bərpa etmək (Restore Purchases)

İstifadəçi tətbiqi silib yenidən qurduqda, ya da yeni cihazda giriş etdikdə, **aktiv abunəliyi aşkar etmək** üçün:

```typescript
import { getAvailablePurchases } from 'react-native-iap';

async function restorePurchases() {
  const purchases = await getAvailablePurchases();

  for (const purchase of purchases) {
    if (purchase.purchaseToken) {
      await verifyPurchaseWithBackend(purchase.productId, purchase.purchaseToken);
    }
  }

  await refreshSubscriptionState();
}
```

Bunu "Ayarlar" ekranında **"Satınalmaları bərpa et"** düyməsi ilə, və ya app ilk açılanda (login-dən sonra) bir dəfə avtomatik çağırmaq tövsiyə olunur.

---

## 11. Xəta və kənar hallar üçün nəzərə alınmalı olanlar

| Hal | Tövsiyə |
|---|---|
| İstifadəçi ödənişi ləğv edir | `purchaseErrorListener`-də sakitcə tut, xəta mesajı göstərmə (bu normal davranışdır) |
| İnternet kəsilir (backend-ə çatmır) | `finishTransaction` çağırma — Google satınalmanı "pending" saxlayacaq, tətbiq növbəti açılışda `purchaseUpdatedListener`-i yenidən işə salacaq |
| Eyni satınalma iki dəfə göndərilir | Backend bunu özü idarə edir (`purchaseTokenHash` ilə axtarış, eyni istifadəçi üçün idempotent) — client tərəfdə əlavə qorunma lazım deyil |
| İstifadəçi planı Pro-dan Pro Plus-a yüksəldir | Google `requestSubscription`-da `subscriptionOffers`/upgrade parametrləri ilə idarə olunur (real satınalma zamanı `linkedPurchaseToken` yaranır) — bu hal üçün ayrıca test lazımdır |
| Test mühitində real pul xərclənməsin | Play Console → License Testing-ə əlavə olunan hesablarla test et (bax bölmə 3.3) |

---

## 12. Xülasə — Backend hissəsi necə istifadə olunacaq

Frontend tərəfi üçün yadda saxlanmalı **yeganə backend əlaqəsi** budur:

1. **`GET /api/subscription/plans`** *(token lazım deyil)* — Planlar ekranını qurmaq üçün (ad, qiymət, xüsusiyyətlər).
2. İstifadəçi plan seçir → **Google Play SDK** (`requestSubscription`) native ödəniş pəncərəsini açır → Google `purchaseToken` qaytarır.
3. **`POST /api/subscription/google/verify`** *(JWT tələb olunur, `Authorization: Bearer {accessToken}`)* — body: `{ "productId": "...", "purchaseToken": "..." }`. Backend bunu Google ilə doğrulayır və cavab olaraq yenilənmiş `SubscriptionResponse`-u qaytarır.
4. Client `finishTransaction` çağırır (yalnız 3-cü addım uğurlu olduqdan sonra).
5. Client **`GET /api/subscription`** və **`GET /api/subscription/usage`**-i yeniləyir ki, UI (Planlar, Əsas səhifə, Ayarlar) cari vəziyyəti göstərsin.
6. **`POST /api/subscription/google/rtdn`** — bu, **client-in heç vaxt çağırmadığı** bir endpoint-dir; Google Cloud Pub/Sub arxa planda avtomatik çağırır (abunəlik yenilənəndə/ləğv olunanda/bitəndə) və backend bazanı özü yeniləyir. Client tərəf yalnız **tətbiq açılanda `GET /api/subscription`-i yenidən çağıraraq** bu dəyişiklikləri görür.

Bütün endpoint-lərin tam sorğu/cavab nümunələri, status kodları və xəta halları üçün → **`POSTMAN_DOCS.md`, bölmə 5 (Subscription)**.
