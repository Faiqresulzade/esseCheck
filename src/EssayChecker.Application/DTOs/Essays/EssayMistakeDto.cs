using EssayChecker.Domain.Enums;

namespace EssayChecker.Application.DTOs.Essays;

public sealed record EssayMistakeDto(
    string Wrong,
    string Correct,
    MistakeCategory Category,
    string Reason);
