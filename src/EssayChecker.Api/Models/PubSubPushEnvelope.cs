namespace EssayChecker.Api.Models;

/// <summary>
/// Google Cloud Pub/Sub push mesajının HTTP body formatı (RTDN bu formatla göndərilir).
/// Sxem Pub/Sub-a məxsusdur, Google Play-ə xas deyil.
/// </summary>
public sealed class PubSubPushEnvelope
{
    public PubSubMessage Message { get; set; } = null!;

    public string? Subscription { get; set; }
}

public sealed class PubSubMessage
{
    /// <summary>Base64 ilə kodlanmış RTDN JSON payload-ı.</summary>
    public string Data { get; set; } = null!;

    public string MessageId { get; set; } = null!;

    public DateTime? PublishTime { get; set; }
}
