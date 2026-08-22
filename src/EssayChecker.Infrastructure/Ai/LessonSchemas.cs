using System.Text.Json;

namespace EssayChecker.Infrastructure.Ai;

/// <summary>
/// Dərs generasiyası üçün strict struktur çıxış sxemi. strict rejimdə hər obyektdə
/// additionalProperties=false və BÜTÜN sahələr required olmalıdır — buna görə burada da
/// "istifadə olunmayan" sahələr (formula, comparison, examples...) sxemdə məcburidir və model
/// onları null / boş massiv kimi qaytarır. Bu, təsadüf deyil: frontend sənədi (§3.1) məhz
/// bütün sahələrin həmişə mövcud olmasını tələb edir.
///
/// Qeyd: strict rejim minItems/maxItems dəstəkləmir, ona görə slayd sayı (6-8) və test sayı (3)
/// yalnız promptla istənilir, dekoderlə zəmanət verilmir.
/// </summary>
internal static class LessonSchemas
{
    public static readonly object Lesson = Parse("""
    {
      "type": "json_schema",
      "json_schema": {
        "name": "english_lesson",
        "strict": true,
        "schema": {
          "type": "object",
          "additionalProperties": false,
          "required": ["isEnglishTopic", "slides", "quiz"],
          "properties": {
            "isEnglishTopic": { "type": "boolean" },
            "slides": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["type", "title", "body", "formula", "keywords", "examples", "mistakes", "comparison", "points"],
                "properties": {
                  "type": { "type": "string",
                            "enum": ["Intro", "Rule", "Examples", "Mistakes", "Compare", "Summary"] },
                  "title": { "type": "string" },
                  "body": { "type": ["string", "null"] },
                  "formula": { "type": ["string", "null"] },
                  "keywords": { "type": "array", "items": { "type": "string" } },
                  "examples": {
                    "type": "array",
                    "items": {
                      "type": "object",
                      "additionalProperties": false,
                      "required": ["en", "az", "highlight"],
                      "properties": {
                        "en": { "type": "string" },
                        "az": { "type": "string" },
                        "highlight": { "type": ["string", "null"] }
                      }
                    }
                  },
                  "mistakes": {
                    "type": "array",
                    "items": {
                      "type": "object",
                      "additionalProperties": false,
                      "required": ["wrong", "correct", "note"],
                      "properties": {
                        "wrong": { "type": "string" },
                        "correct": { "type": "string" },
                        "note": { "type": "string" }
                      }
                    }
                  },
                  "comparison": {
                    "type": ["object", "null"],
                    "additionalProperties": false,
                    "required": ["leftTitle", "leftBody", "rightTitle", "rightBody"],
                    "properties": {
                      "leftTitle": { "type": "string" },
                      "leftBody": { "type": "string" },
                      "rightTitle": { "type": "string" },
                      "rightBody": { "type": "string" }
                    }
                  },
                  "points": { "type": "array", "items": { "type": "string" } }
                }
              }
            },
            "quiz": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["question", "options", "correctIndex", "explanation"],
                "properties": {
                  "question": { "type": "string" },
                  "options": { "type": "array", "items": { "type": "string" } },
                  "correctIndex": { "type": "integer" },
                  "explanation": { "type": "string" }
                }
              }
            }
          }
        }
      }
    }
    """);

    private static object Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
