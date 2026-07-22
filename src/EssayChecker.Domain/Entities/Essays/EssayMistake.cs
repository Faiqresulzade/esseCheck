using EssayChecker.Domain.Enums;

namespace EssayChecker.Domain.Entities.Essays;

/// <summary>Tapılan bir səhv: səhv mətn → düzəliş + kateqoriya + səbəb.</summary>
public class EssayMistake
{
    public string Wrong { get; set; } = null!;
    public string Correct { get; set; } = null!;
    public MistakeCategory Category { get; set; }
    public string Reason { get; set; } = null!;
}
