namespace EssayChecker.Domain.Enums;

/// <summary>Abunəliyin mənbəyi. Google Play Billing gələcəkdə əlavə olunacaq.</summary>
public enum SubscriptionPlatform
{
    Manual = 0,
    GooglePlay = 1,
    AppStore = 2,

    /// <summary>
    /// Qeydiyyatda avtomatik verilən pulsuz sınaq abunəliyi. Real satınalmadan ayrılır ki,
    /// hesabatlarda gəlir kimi sayılmasın və lazım gələndə ayrıca filtrlənə bilsin.
    /// </summary>
    Trial = 3
}
