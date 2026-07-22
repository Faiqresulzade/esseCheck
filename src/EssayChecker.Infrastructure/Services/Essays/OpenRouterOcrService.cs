using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using EssayChecker.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Essays;

internal sealed class OpenRouterOcrService : IOcrService
{
    private readonly OpenRouterClient _client;
    private readonly OpenRouterSettings _settings;

    public OpenRouterOcrService(OpenRouterClient client, IOptions<OpenRouterSettings> options)
    {
        _client = client;
        _settings = options.Value;
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

        var text = await _client.CompleteAsync(_settings.OcrModel, messages, cancellationToken);
        return text.Trim();
    }
}
