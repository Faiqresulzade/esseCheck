namespace EssayChecker.Application.DTOs.Essays;

public sealed record EssayScoresDto(
    double Structure,
    string StructureComment,
    double Content,
    string ContentComment,
    double Grammar,
    string GrammarComment,
    double Vocabulary,
    string VocabularyComment,
    double Total);
