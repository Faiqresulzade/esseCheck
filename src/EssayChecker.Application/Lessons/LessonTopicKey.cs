namespace EssayChecker.Application.Lessons;

/// <summary>
/// Mövzu normalizasiyası — keş və təkrar aşkarlaması üçün tək mənbə. "Present Perfect",
/// "present  perfect" və "  PRESENT PERFECT " eyni açara düşməlidir, əks halda keş demək olar
/// ki, heç vaxt işə düşmür.
/// </summary>
public static class LessonTopicKey
{
    public static string Normalize(string topic) =>
        string.Join(' ', topic.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
}
