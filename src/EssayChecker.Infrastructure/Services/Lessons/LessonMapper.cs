using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Domain.Entities.Lessons;

namespace EssayChecker.Infrastructure.Services.Lessons;

/// <summary>Domen entiteti ↔ DTO çevrilməsi. Məzmuna heç bir düzəliş etmir.</summary>
internal static class LessonMapper
{
    public static List<LessonSlide> ToEntity(IReadOnlyList<LessonSlideDto> slides) =>
        slides.Select(s => new LessonSlide
        {
            Type = s.Type,
            Title = s.Title,
            Body = s.Body,
            Formula = s.Formula,
            Keywords = s.Keywords.ToList(),
            Examples = s.Examples
                .Select(e => new LessonExample { En = e.En, Az = e.Az, Highlight = e.Highlight })
                .ToList(),
            Mistakes = s.Mistakes
                .Select(m => new LessonMistakeItem { Wrong = m.Wrong, Correct = m.Correct, Note = m.Note })
                .ToList(),
            Comparison = s.Comparison is null
                ? null
                : new LessonComparison
                {
                    LeftTitle = s.Comparison.LeftTitle,
                    LeftBody = s.Comparison.LeftBody,
                    RightTitle = s.Comparison.RightTitle,
                    RightBody = s.Comparison.RightBody
                },
            Points = s.Points.ToList()
        }).ToList();

    public static List<LessonQuizQuestion> ToEntity(IReadOnlyList<LessonQuizQuestionDto> quiz) =>
        quiz.Select(q => new LessonQuizQuestion
        {
            Question = q.Question,
            Options = q.Options.ToList(),
            CorrectIndex = q.CorrectIndex,
            Explanation = q.Explanation
        }).ToList();

    public static LessonResponse ToResponse(Lesson lesson, string createdByName, bool isMine) =>
        new(
            lesson.Id,
            lesson.Topic,
            lesson.Grade,
            createdByName,
            isMine,
            lesson.CreatedAt,
            ToDto(lesson.Slides),
            ToDto(lesson.Quiz));

    public static IReadOnlyList<LessonSlideDto> ToDto(List<LessonSlide> slides) =>
        slides.Select(s => new LessonSlideDto(
            s.Type,
            s.Title,
            s.Body,
            s.Formula,
            s.Keywords,
            s.Examples.Select(e => new LessonExampleDto(e.En, e.Az, e.Highlight)).ToList(),
            s.Mistakes.Select(m => new LessonMistakeDto(m.Wrong, m.Correct, m.Note)).ToList(),
            s.Comparison is null
                ? null
                : new LessonComparisonDto(
                    s.Comparison.LeftTitle, s.Comparison.LeftBody,
                    s.Comparison.RightTitle, s.Comparison.RightBody),
            s.Points)).ToList();

    public static IReadOnlyList<LessonQuizQuestionDto> ToDto(List<LessonQuizQuestion> quiz) =>
        quiz.Select(q => new LessonQuizQuestionDto(q.Question, q.Options, q.CorrectIndex, q.Explanation)).ToList();
}
