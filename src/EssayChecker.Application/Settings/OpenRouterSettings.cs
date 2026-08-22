using System.ComponentModel.DataAnnotations;

namespace EssayChecker.Application.Settings;

public sealed class OpenRouterSettings
{
    public const string SectionName = "OpenRouter";

    [Required]
    public string ApiKey { get; set; } = null!;

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";

    /// <summary>
    /// Esse qiymətləndirmə üçün əsas model (OpenRouter model id). Keyfiyyət/etibarlılıq üçün
    /// qəsdən pullu model istifadə olunur — pulsuz modellər auto-router randomluğu və
    /// "reasoning" token israfı kimi qeyri-sabitliklər göstərir.
    /// </summary>
    [Required]
    public string Model { get; set; } = null!;

    /// <summary>
    /// Şəkildən mətn oxumaq (OCR) üçün vision model. Qəsdən pulsuz model (xərci azaltmaq üçün) —
    /// pulsuz vision modellərin OpenRouter-də paylaşılan, kiçik kvotası olduğu üçün rate-limitə
    /// düşmə riski var, ona görə <see cref="OcrFallbackModel"/> mütləq təyin olunmalıdır.
    /// </summary>
    [Required]
    public string OcrModel { get; set; } = null!;

    /// <summary>
    /// OcrModel uğursuz olduqda (rate-limit, timeout, keçici xəta) müraciət olunacaq pullu
    /// ehtiyat model. Opsionaldır — boş qalarsa fallback aktivləşmir.
    /// </summary>
    public string? OcrFallbackModel { get; set; }

    /// <summary>
    /// Mövzu izahı (dərs) generasiyası üçün model. Esse modelindən qəsdən AYRIDIR: dərs çıxışı
    /// esse cavabından xeyli uzundur (6-8 slayd + test), ona görə model seçimi ayrıca xərc
    /// qərarıdır. Keyfiyyət kifayət etməsə yalnız bu sətri dəyişmək kifayətdir.
    /// Uğursuz olduqda <see cref="FallbackModel"/> sınanır.
    /// </summary>
    [Required]
    public string LessonModel { get; set; } = null!;

    /// <summary>
    /// Əsas model (Model) uğursuz olduqda (JSON xətası, keçici xəta, ya da OpenRouter
    /// kreditinin bitməsi — 402) müraciət olunacaq ehtiyat model. Qəsdən pulsuz model seçilib
    /// ki, kredit qurtaranda xidmət tamamilə dayanmasın. Opsionaldır — boş qalarsa fallback
    /// aktivləşmir. Qəsdən [Required] deyil ki, bu doldurulmayanda tətbiqin qalan hissəsi
    /// bloklanmasın (GooglePlaySettings ilə eyni prinsip).
    /// </summary>
    public string? FallbackModel { get; set; }

    /// <summary>
    /// 0 = maksimum determinizm. Qiymətləndirmə subyektiv "yaradıcılıq" deyil, sabit rubrikaya
    /// əsaslanan ölçmədir — eyni esse hər dəfə eyni nəticəni verməlidir.
    /// DİQQƏT: bəzi modellər (o cümlədən hazırkı gpt-5.6-luna) bu parametri dəstəkləmir və
    /// səssizcə nəzərə almır — belə modellərdə determinizmi <see cref="Seed"/> təmin edir.
    /// </summary>
    public float Temperature { get; set; } = 0f;

    /// <summary>
    /// Sabit sample seed. Temperature dəstəklənmədikdə eyni essenin hər dəfə fərqli bal/səhv sayı
    /// alması buna görə qarşısı alınır: seed təsadüfiliyi söndürmür, onu təkrarlanan edir.
    /// Dəyər qəsdən konfiqurasiyadadır ki, lazım gələndə dəyişdirilə bilsin (məs. modelin bir
    /// esseyə ilişib qalan pis cavabından çıxmaq üçün).
    /// Boş buraxılarsa sahə sorğuya ümumiyyətlə əlavə olunmur — dəstəkləməyən modellər üçün
    /// təhlükəsiz defolt. OpenAI bunu "best effort" adlandırır: model versiyası dəyişəndə və ya
    /// sorğu başqa provayderə düşəndə determinizm poza bilər.
    /// ÖLÇÜLÜB (2026-08-20, gpt-5.6-luna): bu modeldə seed HEÇ BİR təsir vermir — eyni esse, eyni
    /// seed ilə 9 qaçışın 9-u da fərqli nəticə verdi, sorğu isə xətasız qəbul olunur (cavabda
    /// system_fingerprint null gəlir). Ona görə qəsdən boş saxlanılır: kod hazırdır, model
    /// dəyişdirildikdə sadəcə konfiqurasiyada doldurmaq kifayətdir.
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// Çıxış token limiti. Yüksək dəyər generasiya vaxtını artırır (autoregressive modellərdə
    /// cavab vaxtı çıxış token sayı ilə düz mütənasibdir) — 5000 simvollu maksimum esse üçün
    /// 4096 kifayət qədər genişdir, artırma performansı əhəmiyyətli dərəcədə pisləşdirir.
    /// </summary>
    [Range(1, 100000)]
    public int MaxTokens { get; set; } = 4096;

    /// <summary>OpenRouter reytinqləri üçün opsional başlıqlar.</summary>
    public string? Referer { get; set; }

    public string? Title { get; set; } = "EssayCheck AI";
}
