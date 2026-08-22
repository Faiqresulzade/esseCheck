using System.Diagnostics;
using System.Text.Json;
using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.DTOs.Lessons;
using EssayChecker.Application.Settings;
using EssayChecker.Domain.Enums;
using EssayChecker.Infrastructure.Ai;
using EssayChecker.Infrastructure.Services.Essays;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Lessons;

/// <summary>
/// Dərsi bir AI çağırışı ilə yaradır. Mövzunun uyğunluğu ayrıca çağırışla yoxlanmır —
/// cavabın özündə <c>isEnglishTopic</c> gəlir (esse axınındakı <c>isEssay</c> ilə eyni şablon),
/// beləliklə uyğunsuz mövzu üçün əlavə xərc olmur.
/// </summary>
internal sealed class OpenRouterLessonGenerator : ILessonGenerator
{
    private readonly OpenRouterClient _client;
    private readonly OpenRouterSettings _settings;
    private readonly ILogger<OpenRouterLessonGenerator> _logger;

    public OpenRouterLessonGenerator(
        OpenRouterClient client,
        IOptions<OpenRouterSettings> options,
        ILogger<OpenRouterLessonGenerator> logger)
    {
        _client = client;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<LessonGenerationResult> GenerateAsync(
        string topic, GradeLevel grade, CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(topic, grade);
        var response = await CallWithFallbackAsync(messages, cancellationToken);

        if (!response.IsEnglishTopic)
            return new LessonGenerationResult(false, Array.Empty<LessonSlideDto>(), Array.Empty<LessonQuizQuestionDto>());

        return new LessonGenerationResult(
            true,
            LessonContentMapper.MapSlides(response.Slides),
            LessonContentMapper.MapQuiz(response.Quiz));
    }

    /// <summary>
    /// Əvvəlcə LessonModel, uğursuz olarsa (və konfiqurasiya olunubsa) bir dəfə FallbackModel.
    /// Siyahı ən çoxu 2 elementdir, ona görə dövr sonsuza gedə bilməz.
    /// </summary>
    private async Task<AiLessonResponse> CallWithFallbackAsync(
        ChatMessage[] messages, CancellationToken cancellationToken)
    {
        var modelsToTry = string.IsNullOrWhiteSpace(_settings.FallbackModel)
            ? new[] { _settings.LessonModel }
            : new[] { _settings.LessonModel, _settings.FallbackModel };

        Exception? lastError = null;
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < modelsToTry.Length; i++)
        {
            var model = modelsToTry[i];
            var attemptStart = stopwatch.Elapsed;
            try
            {
                var raw = await _client.CompleteAsync(model, messages, cancellationToken, LessonSchemas.Lesson);
                var parsed = AiEssayResponseParser.Parse<AiLessonResponse>(raw);

                if (i > 0)
                {
                    _logger.LogWarning(
                        "Dərs generasiyası ehtiyat modelə ({Model}) keçdi və uğurlu oldu (bu cəhd {ElapsedMs}ms, cəmi {TotalMs}ms).",
                        model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds, stopwatch.ElapsedMilliseconds);
                }

                return parsed;
            }
            catch (JsonException ex)
            {
                lastError = ex;
                _logger.LogWarning(ex,
                    "Dərs cavabı JSON parse edilmədi (model {Model}, bu cəhd {ElapsedMs}ms).",
                    model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds);
            }
            catch (AiServiceException ex) when (ex.IsTransient)
            {
                lastError = ex;
                _logger.LogWarning(ex,
                    "Dərs generasiyasında keçici xəta (model {Model}, bu cəhd {ElapsedMs}ms).",
                    model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds);
            }
        }

        throw new AiServiceException(
            $"Dərs generasiyası bütün modellərdən ({string.Join(", ", modelsToTry)}) sonra uğursuz oldu: {lastError?.Message}",
            isTransient: true,
            innerException: lastError);
    }

    /// <summary>
    /// Sistem promptu iki hissəyə bölünür: sabit qaydalar (cache_control ilə keşlənir, hər
    /// sorğuda bayt-bayt eynidir) və mövzuya/sinfə görə dəyişən blok.
    /// </summary>
    private static ChatMessage[] BuildMessages(string topic, GradeLevel grade) =>
    [
        new ChatMessage
        {
            Role = "system",
            Content = new[]
            {
                new TextContentPart { Text = LessonPrompts.Rules, CacheControl = new CacheControl() },
                new TextContentPart { Text = LessonPrompts.GetInput(topic, grade) }
            }
        },
        new ChatMessage { Role = "user", Content = topic }
    ];
}
