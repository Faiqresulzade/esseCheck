using System.Text.Json;

namespace EssayChecker.Infrastructure.Ai;

/// <summary>
/// OpenAI "strict" struktur çıxış sxemləri. Dəstəkləyən modellərdə JSON forması modelin öz
/// iradəsindən deyil, dekoderdən asılı olur. strict rejimin tələbləri: hər obyektdə
/// additionalProperties=false və BÜTÜN sahələr required siyahısında olmalıdır.
/// </summary>
internal static class EssaySchemas
{
    private static readonly JsonDocumentOptions DocumentOptions = new();

    public static readonly object Detection = Parse("""
    {
      "type": "json_schema",
      "json_schema": {
        "name": "essay_mistakes",
        "strict": true,
        "schema": {
          "type": "object",
          "additionalProperties": false,
          "required": ["isEssay", "mistakes"],
          "properties": {
            "isEssay": { "type": "boolean" },
            "mistakes": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["wrong", "correct", "category", "reason"],
                "properties": {
                  "wrong":    { "type": "string" },
                  "correct":  { "type": "string" },
                  "category": { "type": "string",
                                "enum": ["Grammar", "Spelling", "Vocabulary", "NaturalExpression"] },
                  "reason":   { "type": "string" }
                }
              }
            }
          }
        }
      }
    }
    """);

    public static readonly object Scoring = Parse("""
    {
      "type": "json_schema",
      "json_schema": {
        "name": "essay_scores",
        "strict": true,
        "schema": {
          "type": "object",
          "additionalProperties": false,
          "required": ["scores", "teacherFeedback"],
          "properties": {
            "scores": {
              "type": "object",
              "additionalProperties": false,
              "required": ["structure","structureComment","content","contentComment",
                           "grammar","grammarComment","vocabulary","vocabularyComment"],
              "properties": {
                "structure":         { "type": "number" },
                "structureComment":  { "type": "string" },
                "content":           { "type": "number" },
                "contentComment":    { "type": "string" },
                "grammar":           { "type": "number" },
                "grammarComment":    { "type": "string" },
                "vocabulary":        { "type": "number" },
                "vocabularyComment": { "type": "string" }
              }
            },
            "teacherFeedback": {
              "type": "object",
              "additionalProperties": false,
              "required": ["strengths", "weaknesses", "recommendations"],
              "properties": {
                "strengths":       { "type": "array", "items": { "type": "string" } },
                "weaknesses":      { "type": "array", "items": { "type": "string" } },
                "recommendations": { "type": "array", "items": { "type": "string" } }
              }
            }
          }
        }
      }
    }
    """);

    /// <summary>JsonDocument yerinə klonlanmış JsonElement — static sahədə saxlanıldığı üçün
    /// sənədin ömrü boyu dispose olunmamalıdır və serializasiya zamanı təkrar-təkrar oxunur.</summary>
    private static object Parse(string schemaJson)
    {
        using var document = JsonDocument.Parse(schemaJson, DocumentOptions);
        return document.RootElement.Clone();
    }
}
