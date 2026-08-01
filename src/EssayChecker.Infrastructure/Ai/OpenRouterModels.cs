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

    /// <summary>Mətn üçün string, vision (OCR) üçün content-part massivi.</summary>
    [JsonPropertyName("content")]
    public object Content { get; set; } = null!;
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
