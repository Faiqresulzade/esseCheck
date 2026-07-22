using System.Net.Http.Json;
using System.Text.Json;
using EssayChecker.Application.Common;
using EssayChecker.Application.Settings;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Ai;

/// <summary>OpenRouter chat/completions çağırışlarını idarə edən ortaq client.</summary>
internal sealed class OpenRouterClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly OpenRouterSettings _settings;

    public OpenRouterClient(HttpClient http, IOptions<OpenRouterSettings> options)
    {
        _http = http;
        _settings = options.Value;
    }

    public async Task<string> CompleteAsync(
        string model,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var payload = new ChatCompletionRequest
        {
            Model = model,
            Messages = messages,
            Temperature = _settings.Temperature,
            MaxTokens = _settings.MaxTokens
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl)
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _settings.ApiKey);
        if (!string.IsNullOrWhiteSpace(_settings.Referer))
            request.Headers.TryAddWithoutValidation("HTTP-Referer", _settings.Referer);
        if (!string.IsNullOrWhiteSpace(_settings.Title))
            request.Headers.TryAddWithoutValidation("X-Title", _settings.Title);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiServiceException("AI xidmətindən cavab vaxtı bitdi (timeout).", isTransient: true);
        }
        catch (HttpRequestException ex)
        {
            throw new AiServiceException("AI xidmətinə qoşulmaq mümkün olmadı.", isTransient: true, ex);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                var isTransient = status == 429 || status >= 500;
                throw new AiServiceException($"OpenRouter error {status}: {body}", isTransient);
            }

            ChatCompletionResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(body, SerializerOptions);
            }
            catch (JsonException ex)
            {
                throw new AiServiceException("AI cavabı gözlənilən formatda deyil.", isTransient: true, ex);
            }

            var content = parsed?.Choices is { Count: > 0 } ? parsed.Choices[0].Message?.Content : null;
            if (string.IsNullOrWhiteSpace(content))
                throw new AiServiceException("OpenRouter returned an empty response.");

            return content;
        }
    }
}
