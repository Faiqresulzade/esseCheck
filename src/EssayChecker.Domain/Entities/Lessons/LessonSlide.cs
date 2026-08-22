using EssayChecker.Domain.Enums;

namespace EssayChecker.Domain.Entities.Lessons;

/// <summary>
/// Bir dərs slaydı. Sahələrin hamısı hər slaydda mövcuddur, sadəcə istifadə olunmayanlar boş
/// qalır (null / boş siyahı) — frontend "sahə yoxdur" halını yoxlamasın deyə.
/// </summary>
public class LessonSlide
{
    public LessonSlideType Type { get; set; }

    public string Title { get; set; } = null!;

    /// <summary>1-3 cümləlik izah (azərbaycanca). Intro, Rule, Compare slaydlarında.</summary>
    public string? Body { get; set; }

    /// <summary>Qısa formul, məs. "have / has + V3". Yalnız Rule slaydında.</summary>
    public string? Formula { get; set; }

    /// <summary>Açar sözlər — tətbiqdə ardıcıl işıqlandırılır.</summary>
    public List<string> Keywords { get; set; } = new();

    public List<LessonExample> Examples { get; set; } = new();

    public List<LessonMistakeItem> Mistakes { get; set; } = new();

    public LessonComparison? Comparison { get; set; }

    /// <summary>Summary slaydındakı yekun cümlələr.</summary>
    public List<string> Points { get; set; } = new();
}

/// <summary>Nümunə cümlə. <see cref="En"/> tətbiqdə TTS ilə səsləndirilir.</summary>
public class LessonExample
{
    public string En { get; set; } = null!;

    public string Az { get; set; } = null!;

    /// <summary>
    /// <see cref="En"/> cümləsinin vurğulanacaq alt-sətri. AI hərfi uyğunluq verməsə frontend
    /// sadəcə vurğulamır — ona görə burada təmizləmə aparılmır.
    /// </summary>
    public string? Highlight { get; set; }
}

/// <summary>Tez-tez edilən səhv: yanlış forma → düzgün forma + izah.</summary>
public class LessonMistakeItem
{
    public string Wrong { get; set; } = null!;

    public string Correct { get; set; } = null!;

    public string Note { get; set; } = null!;
}

/// <summary>İki sütunlu müqayisə (məs. Present Perfect ↔ Past Simple).</summary>
public class LessonComparison
{
    public string LeftTitle { get; set; } = null!;

    public string LeftBody { get; set; } = null!;

    public string RightTitle { get; set; } = null!;

    public string RightBody { get; set; } = null!;
}

/// <summary>Mini testin bir sualı — 4 variant, düzgün cavabın indeksi 0-3.</summary>
public class LessonQuizQuestion
{
    public string Question { get; set; } = null!;

    public List<string> Options { get; set; } = new();

    public int CorrectIndex { get; set; }

    public string Explanation { get; set; } = null!;
}
