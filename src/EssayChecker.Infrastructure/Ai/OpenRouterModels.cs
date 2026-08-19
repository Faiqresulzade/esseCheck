using System.Text.Json.Serialization;

namespace EssayChecker.Infrastructure.Ai;

internal sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    [JsonPropertyName("messages")]
    public IReadOnlyList<ChatMessage> Messages { get; set; } = Array.Empty<ChatMessage>();

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    /// <summary>
    /// Bəzi (əsasən pulsuz) modellər "reasoning" (daxili düşünmə) modelləridir və cavab yazmazdan
    /// əvvəl min-min token sərf edərək düşünürlər — uzun essedə (~600 söz) bu, bütün MaxTokens
    /// büdcəsini yeyib əsl JSON cavabını heç yazdırmır (content: null, finish_reason: "length").
    /// Reasoning-i söndürmək bunun qarşısını alır və həm sürəti, həm etibarlılığı artırır.
    /// </summary>
    [JsonPropertyName("reasoning")]
    public ReasoningOptions Reasoning { get; set; } = new();

    /// <summary>
    /// Struktur çıxış sxemi (bax <see cref="EssaySchemas"/>). Dəstəkləyən modellərdə JSON forması
    /// modelin öz iradəsindən deyil, dekoderdən asılı olur. Dəstəkləməyən modellərdə OpenRouter
    /// sahəni sadəcə buraxır — ona görə parser-in ExtractJson ehtiyatı yerində qalır.
    /// </summary>
    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ResponseFormat { get; set; }
}

internal sealed class ReasoningOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;
}

internal sealed class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    /// <summary>
    /// Mətn üçün string, vision (OCR) üçün content-part massivi, keşlənən sistem promptu üçün
    /// <see cref="TextContentPart"/> massivi (cache_control ilə).
    /// </summary>
    [JsonPropertyName("content")]
    public object Content { get; set; } = null!;
}

/// <summary>
/// Anthropic prompt caching üçün mətn bloku. cache_control doldurulmuş blokdan əvvəlki (bu blok
/// daxil) bütün mətn Anthropic tərəfindən keşlənir — sonrakı sorğularda eyni prefiks üçün
/// prompt token qiyməti ~90% ucuzlaşır. OpenRouter bu sahəni Anthropic modellərinə şəffaf ötürür.
/// </summary>
internal sealed class TextContentPart
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("cache_control")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CacheControl? CacheControl { get; set; }
}

internal sealed class CacheControl
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "ephemeral";

    /// <summary>
    /// "5m" (defolt, boş qalarsa) və ya "1h". 1h yazma qiyməti 5m-dən bahadır (~1.6x), amma
    /// esse sorğuları arasında boşluqlar 5 dəqiqədən uzun ola bildiyi üçün (aktiv istifadəçi
    /// azdırsa) 1h ümumi orta xərci real şəkildə aşağı salır — empirik test edilib: 6.5 dəqiqə
    /// sonra "1h" ilə yazılmış keş hələ də isti idi (cached_tokens dolu), "5m" ilə olsaydı
    /// artıq bitmiş olardı.
    /// </summary>
    [JsonPropertyName("ttl")]
    public string Ttl { get; set; } = "1h";
}

/// <summary>
/// İstifadəçi mesajında mətn+şəkil qarışığı üçün (9-cu sinif, promt-şəkilləri). OCR-də
/// artıq eyni məntiq anonim obyektlə edilib — burada typed versiya, çünki bu sinif
/// (evaluate axını) DTO-ları başqa yerlərdə də sərf olunur və JSON sahə adlarının
/// dəqiq uyğunluğu vacibdir.
/// </summary>
internal sealed class TextOrImageContentPart
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImageUrlPart? ImageUrl { get; set; }
}

internal sealed class ImageUrlPart
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
}

internal sealed class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<ChatChoice>? Choices { get; set; }
}

internal sealed class ChatChoice
{
    [JsonPropertyName("message")]
    public ChatResponseMessage? Message { get; set; }
}

internal sealed class ChatResponseMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
