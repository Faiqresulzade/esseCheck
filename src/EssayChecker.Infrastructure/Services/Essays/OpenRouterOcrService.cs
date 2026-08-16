using System.Diagnostics;
using EssayChecker.Application.Common;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using EssayChecker.Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Essays;

internal sealed class OpenRouterOcrService : IOcrService
{
    private readonly OpenRouterClient _client;
    private readonly OpenRouterSettings _settings;
    private readonly ILogger<OpenRouterOcrService> _logger;

    public OpenRouterOcrService(OpenRouterClient client, IOptions<OpenRouterSettings> options, ILogger<OpenRouterOcrService> logger)
    {
        _client = client;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(
        Stream imageStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await imageStream.CopyToAsync(memory, cancellationToken);
        var base64 = Convert.ToBase64String(memory.ToArray());
        var dataUrl = $"data:{contentType};base64,{base64}";

        var messages = new[]
        {
            new ChatMessage
            {
                Role = "user",
                Content = new object[]
                {
                    new { type = "text", text = EssayPrompts.Ocr },
                    new { type = "image_url", image_url = new { url = dataUrl } }
                }
            }
        };

        // OcrModel əsas (pullu, etibarlı) modeldir — pulsuz vision modellərin transkripsiya
        // keyfiyyəti aşağı olduğu üçün (mətni özbaşına qısaldıb təhrif etdiyi müşahidə edilib)
        // keyfiyyət üstünlük təşkil edir. Yalnız əsas model keçici xəta versə (məs. kredit
        // bitməsi = 402, rate-limit = 429) OcrFallbackModel (pulsuz) sınanır — ən çoxu 2 cəhd,
        // essay-evaluator ilə eyni fallback naxışı.
        var modelsToTry = string.IsNullOrWhiteSpace(_settings.OcrFallbackModel)
            ? new[] { _settings.OcrModel }
            : new[] { _settings.OcrModel, _settings.OcrFallbackModel };

        Exception? lastError = null;
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < modelsToTry.Length; i++)
        {
            var model = modelsToTry[i];
            var isFallback = i > 0;
            var attemptStart = stopwatch.Elapsed;
            try
            {
                var text = await _client.CompleteAsync(model, messages, cancellationToken);

                if (isFallback)
                {
                    _logger.LogWarning(
                        "Pulsuz OCR modeli uğursuz olduğu üçün pullu ehtiyat modelə ({Model}) yönləndirildi və uğurlu oldu (bu cəhd {ElapsedMs}ms, cəmi {TotalMs}ms).",
                        model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds, stopwatch.ElapsedMilliseconds);
                }

                return text.Trim();
            }
            catch (AiServiceException ex) when (ex.IsTransient)
            {
                lastError = ex;
                _logger.LogWarning(ex,
                    "OCR modeli keçici xəta verdi (model {Model}, bu cəhd {ElapsedMs}ms).",
                    model, (stopwatch.Elapsed - attemptStart).TotalMilliseconds);
            }
            // Transient olmayan AiServiceException burada tutulmur — ehtiyat modelə keçmək
            // mənasızdır, dərhal yuxarı ötürülür.
        }

        throw new AiServiceException(
            $"OCR bütün modellərdən ({string.Join(", ", modelsToTry)}) sonra uğursuz oldu: {lastError?.Message}",
            isTransient: true,
            innerException: lastError);
    }
}
