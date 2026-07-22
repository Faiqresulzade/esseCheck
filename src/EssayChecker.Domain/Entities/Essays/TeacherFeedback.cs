namespace EssayChecker.Domain.Entities.Essays;

/// <summary>Müəllim rəyi: güclü tərəflər, zəif tərəflər, tövsiyələr.</summary>
public class TeacherFeedback
{
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}
