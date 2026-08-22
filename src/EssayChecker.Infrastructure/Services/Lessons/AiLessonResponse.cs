using System.Text.Json.Serialization;

namespace EssayChecker.Infrastructure.Services.Lessons;

/// <summary>AI-dan gələn xam dərs cavabı (bax <see cref="Ai.LessonSchemas"/>).</summary>
internal sealed class AiLessonResponse
{
    [JsonPropertyName("isEnglishTopic")]
    public bool IsEnglishTopic { get; set; }

    [JsonPropertyName("slides")]
    public List<AiLessonSlide>? Slides { get; set; }

    [JsonPropertyName("quiz")]
    public List<AiQuizQuestion>? Quiz { get; set; }
}

internal sealed class AiLessonSlide
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("formula")]
    public string? Formula { get; set; }

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }

    [JsonPropertyName("examples")]
    public List<AiLessonExample>? Examples { get; set; }

    [JsonPropertyName("mistakes")]
    public List<AiLessonMistake>? Mistakes { get; set; }

    [JsonPropertyName("comparison")]
    public AiLessonComparison? Comparison { get; set; }

    [JsonPropertyName("points")]
    public List<string>? Points { get; set; }
}

internal sealed class AiLessonExample
{
    [JsonPropertyName("en")]
    public string? En { get; set; }

    [JsonPropertyName("az")]
    public string? Az { get; set; }

    [JsonPropertyName("highlight")]
    public string? Highlight { get; set; }
}

internal sealed class AiLessonMistake
{
    [JsonPropertyName("wrong")]
    public string? Wrong { get; set; }

    [JsonPropertyName("correct")]
    public string? Correct { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

internal sealed class AiLessonComparison
{
    [JsonPropertyName("leftTitle")]
    public string? LeftTitle { get; set; }

    [JsonPropertyName("leftBody")]
    public string? LeftBody { get; set; }

    [JsonPropertyName("rightTitle")]
    public string? RightTitle { get; set; }

    [JsonPropertyName("rightBody")]
    public string? RightBody { get; set; }
}

internal sealed class AiQuizQuestion
{
    [JsonPropertyName("question")]
    public string? Question { get; set; }

    [JsonPropertyName("options")]
    public List<string>? Options { get; set; }

    [JsonPropertyName("correctIndex")]
    public int CorrectIndex { get; set; }

    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
}
