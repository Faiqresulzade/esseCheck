using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Application.Lessons;
using EssayChecker.Domain.Entities.Lessons;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.Ai;

namespace EssayChecker.Infrastructure.Services.Lessons;

/// <summary>
/// Dərs axını. Üç mərhələli qənaət:
/// 1. İstifadəçinin öz siyahısında bu mövzu varsa — hazır dərs qaytarılır, nə AI, nə limit.
/// 2. Keşdə (başqasının yaratdığı) şablon varsa — AI çağırılmır, amma limit xərclənir.
/// 3. Yalnız qalan halda AI çağırılır.
/// </summary>
public sealed class LessonService : ILessonService
{
    private readonly ILessonRepository _lessons;
    private readonly ILessonGenerator _generator;
    private readonly IUsageLimitService _usageLimit;
    private readonly ITeachingRepository _teaching;

    public LessonService(
        ILessonRepository lessons,
        ILessonGenerator generator,
        IUsageLimitService usageLimit,
        ITeachingRepository teaching)
    {
        _lessons = lessons;
        _generator = generator;
        _usageLimit = usageLimit;
        _teaching = teaching;
    }

    public async Task<CreateLessonResult> CreateAsync(
        int userId, CreateLessonRequest request, GradeLevel grade, CancellationToken cancellationToken = default)
    {
        var topic = request.Topic.Trim();
        var normalizedTopic = LessonTopicKey.Normalize(topic);

        // 1. Bu mövzu artıq istifadəçinin siyahısındadır? Dublikat yaratmırıq, limit də toxunmuruq.
        var existing = await _lessons.FindOwnAsync(userId, normalizedTopic, grade, cancellationToken);
        if (existing is not null)
            return CreateLessonResult.Reused(await ToResponseAsync(existing, cancellationToken));

        var decision = await _usageLimit.CheckLessonAsync(userId, cancellationToken);
        if (!decision.Allowed)
            return CreateLessonResult.LimitExceeded(decision.Reason ?? "Bugünkü dərs limitiniz bitib.");

        // 2. Keş: eyni mövzu+sinif+prompt versiyası üçün hazır məzmun.
        var template = await _lessons.FindTemplateAsync(
            normalizedTopic, grade, LessonPrompts.Version, cancellationToken);

        List<LessonSlide> slides;
        List<LessonQuizQuestion> quiz;
        var displayTopic = topic;

        if (template is not null)
        {
            slides = LessonMapper.ToEntity(LessonMapper.ToDto(template.Slides));
            quiz = LessonMapper.ToEntity(LessonMapper.ToDto(template.Quiz));
            displayTopic = template.Topic;
        }
        else
        {
            // 3. AI. Mövzu uyğun deyilsə heç nə saxlanmır və sayğac artırılmır.
            var generated = await _generator.GenerateAsync(topic, grade, cancellationToken);
            if (!generated.IsEnglishTopic)
            {
                return CreateLessonResult.InvalidTopic(
                    "Bu mövzu İngilis dili dərsinə aid deyil. İngilis dili ilə bağlı mövzu yazın.");
            }

            slides = LessonMapper.ToEntity(generated.Slides);
            quiz = LessonMapper.ToEntity(generated.Quiz);

            await _lessons.AddTemplateAsync(new LessonTemplate
            {
                NormalizedTopic = normalizedTopic,
                Topic = topic,
                Grade = grade,
                PromptVersion = LessonPrompts.Version,
                CreatedAt = DateTime.UtcNow,
                Slides = LessonMapper.ToEntity(generated.Slides),
                Quiz = LessonMapper.ToEntity(generated.Quiz)
            }, cancellationToken);
        }

        var lesson = new Lesson
        {
            UserId = userId,
            StudentId = request.StudentId,
            Topic = displayTopic,
            NormalizedTopic = normalizedTopic,
            Grade = grade,
            CreatedAt = DateTime.UtcNow,
            Slides = slides,
            Quiz = quiz
        };

        await _lessons.AddAsync(lesson, cancellationToken);

        // Sayğac yalnız dərs həqiqətən saxlandıqdan sonra artır — keşdən gəlsə də artır (§6).
        await _usageLimit.ConsumeLessonAsync(userId, cancellationToken);

        return CreateLessonResult.Created(await ToResponseAsync(lesson, cancellationToken));
    }

    public Task<LessonHistoryResponse> GetHistoryAsync(
        int userId, string? search, int? studentId, int? groupId, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        _lessons.GetHistoryAsync(userId, search, studentId, groupId, page, pageSize, cancellationToken);

    public async Task<LessonResponse?> GetByIdAsync(
        int userId, int lessonId, CancellationToken cancellationToken = default)
    {
        var lesson = await _lessons.GetByIdAsync(userId, lessonId, cancellationToken);
        return lesson is null ? null : await ToResponseAsync(lesson, cancellationToken);
    }

    public Task<bool> DeleteAsync(int userId, int lessonId, CancellationToken cancellationToken = default) =>
        _lessons.DeleteAsync(userId, lessonId, cancellationToken);

    /// <summary>
    /// Şagirdin adı sahiblik yoxlaması olmadan oxunur — dərs onsuz da istifadəçinindir, şagird
    /// isə sonradan silinmiş ola bilər (adı tarixçədə qalmalıdır).
    /// </summary>
    private async Task<LessonResponse> ToResponseAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        var studentName = lesson.StudentId is null
            ? null
            : await _teaching.GetStudentNameAsync(lesson.StudentId.Value, cancellationToken);

        return LessonMapper.ToResponse(lesson, studentName);
    }
}
