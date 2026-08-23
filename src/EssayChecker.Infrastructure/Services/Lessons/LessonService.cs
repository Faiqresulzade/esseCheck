using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Application.Lessons;
using EssayChecker.Domain.Entities.Lessons;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace EssayChecker.Infrastructure.Services.Lessons;

/// <summary>
/// Ortaq dərs kitabxanası.
///
/// Əsas qayda: gündəlik limit yalnız REAL AI xərcini məhdudlaşdırır. Mövzu kitabxanada varsa
/// (kim yaradıbsa fərqi yoxdur) dərs pulsuz açılır — nə AI çağırılır, nə sayğac artır. Limit
/// yalnız kitabxanada olmayan yeni mövzu üçün tutulur.
/// </summary>
public sealed class LessonService : ILessonService
{
    private readonly ILessonRepository _lessons;
    private readonly ILessonGenerator _generator;
    private readonly IUsageLimitService _usageLimit;
    private readonly ILogger<LessonService> _logger;

    public LessonService(
        ILessonRepository lessons,
        ILessonGenerator generator,
        IUsageLimitService usageLimit,
        ILogger<LessonService> logger)
    {
        _lessons = lessons;
        _generator = generator;
        _usageLimit = usageLimit;
        _logger = logger;
    }

    public async Task<CreateLessonResult> CreateAsync(
        int userId, CreateLessonRequest request, GradeLevel grade, CancellationToken cancellationToken = default)
    {
        var topic = request.Topic.Trim();
        var normalizedTopic = LessonTopicKey.Normalize(topic);

        // 1. Kitabxanada var? Onda pulsuzdur — istifadəçi onsuz da siyahıdan açıb oxuya bilərdi.
        var existing = await _lessons.FindByTopicAsync(normalizedTopic, grade, cancellationToken);
        if (existing is not null)
            return CreateLessonResult.AlreadyInLibrary(await ToResponseAsync(existing, userId, cancellationToken));

        var decision = await _usageLimit.CheckLessonAsync(userId, cancellationToken);
        if (!decision.Allowed)
            return CreateLessonResult.LimitExceeded(decision.Reason ?? "Bugünkü dərs limitiniz bitib.");

        // 2. Yalnız burada AI çağırılır. Mövzu uyğun deyilsə heç nə saxlanmır və sayğac artmır.
        var generated = await _generator.GenerateAsync(topic, grade, cancellationToken);
        if (!generated.IsEnglishTopic)
        {
            return CreateLessonResult.InvalidTopic(
                "Bu mövzu İngilis dili dərsinə aid deyil. İngilis dili ilə bağlı mövzu yazın.");
        }

        var lesson = new Lesson
        {
            CreatedByUserId = userId,
            Topic = topic,
            NormalizedTopic = normalizedTopic,
            Grade = grade,
            PromptVersion = LessonPrompts.Version,
            CreatedAt = DateTime.UtcNow,
            Slides = LessonMapper.ToEntity(generated.Slides),
            Quiz = LessonMapper.ToEntity(generated.Quiz)
        };

        try
        {
            await _lessons.AddAsync(lesson, cancellationToken);
        }
        catch (Exception ex) when (IsDuplicateTopic(ex))
        {
            // İki istifadəçi eyni mövzunu eyni anda yazsa unikal indeks pozula bilər. Bu, xəta
            // deyil: rəqib artıq eyni dərsi yaradıb, onu qaytarırıq və limiti TUTMURUQ.
            _logger.LogInformation(
                "Dərs paralel olaraq başqa istifadəçi tərəfindən yaradılıb, mövcud olan qaytarılır: {Topic} ({Grade}).",
                normalizedTopic, grade);

            var winner = await _lessons.FindByTopicAsync(normalizedTopic, grade, cancellationToken);
            if (winner is not null)
                return CreateLessonResult.AlreadyInLibrary(await ToResponseAsync(winner, userId, cancellationToken));

            throw;
        }

        // Sayğac yalnız kitabxanaya HƏQİQƏTƏN yeni dərs əlavə olunduqdan sonra artır.
        await _usageLimit.ConsumeLessonAsync(userId, cancellationToken);

        return CreateLessonResult.Created(await ToResponseAsync(lesson, userId, cancellationToken));
    }

    public Task<LessonHistoryResponse> GetLibraryAsync(
        int userId, string? search, GradeLevel? grade, bool onlyMine, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        _lessons.GetLibraryAsync(userId, search, grade, onlyMine, page, pageSize, cancellationToken);

    public async Task<LessonResponse?> GetByIdAsync(
        int userId, int lessonId, CancellationToken cancellationToken = default)
    {
        // Sahiblik yoxlanmır: kitabxana ortaqdır, hər dərsi hər kəs oxuya bilər.
        var lesson = await _lessons.GetByIdAsync(lessonId, cancellationToken);
        return lesson is null ? null : await ToResponseAsync(lesson, userId, cancellationToken);
    }

    private async Task<LessonResponse> ToResponseAsync(Lesson lesson, int currentUserId, CancellationToken cancellationToken)
    {
        var createdByName = await _lessons.GetCreatorNameAsync(lesson.CreatedByUserId, cancellationToken);
        return LessonMapper.ToResponse(
            lesson,
            LessonCreator.DisplayName(createdByName),
            lesson.CreatedByUserId == currentUserId);
    }

    /// <summary>
    /// Unikal indeks pozuntusunu tanıyır. Npgsql-ə birbaşa istinad etməmək üçün istisna zənciri
    /// mətnə görə yoxlanılır — Infrastructure qatı konkret verilənlər bazası sürücüsündən asılı olmamalıdır.
    /// </summary>
    private static bool IsDuplicateTopic(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException!)
        {
            if (e.GetType().Name.Contains("Postgres", StringComparison.Ordinal) &&
                e.Message.Contains("IX_Lessons_NormalizedTopic_Grade", StringComparison.Ordinal))
            {
                return true;
            }

            if (e.InnerException is null)
                break;
        }

        return false;
    }
}
