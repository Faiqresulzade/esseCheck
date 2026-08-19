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
        CancellationToken cancellationToken,
        object? responseFormat = null)
    {
        var payload = new ChatCompletionRequest
        {
            Model = model,
            Messages = messages,
            Temperature = _settings.Temperature,
            MaxTokens = _settings.MaxTokens,
            ResponseFormat = responseFormat
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
            string body;
            try
            {
                // Başlıqlar (headers) uğurla alınsa da (yuxarıdakı SendAsync bitib), böyük
                // cavabın (məs. OCR/vision) bədənini oxuyarkən bağlantı kəsilə bilər — bu,
                // ayrıca bir keçici xəta mənbəyidir və eyni cür AiServiceException-a
                // çevrilməlidir, yoxsa tutulmamış istisna kimi generic 500-ə düşür.
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new AiServiceException("AI xidmətindən cavab vaxtı bitdi (timeout).", isTransient: true);
            }
            catch (HttpRequestException ex)
            {
                throw new AiServiceException("AI xidmətindən cavab oxunarkən bağlantı kəsildi.", isTransient: true, ex);
            }
            catch (IOException ex)
            {
                throw new AiServiceException("AI xidmətindən cavab oxunarkən bağlantı kəsildi.", isTransient: true, ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                // 402 = kredit qurtarıb ("Payment Required") — bu, əsas (pullu) model üçün
                // ən çox rast gəlinən uğursuzluq halıdır və ehtiyat (pulsuz) modelə keçməyə
                // əsas verir, ona görə keçici sayılır.
                var isTransient = status == 402 || status == 429 || status >= 500;
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
                throw new AiServiceException("OpenRouter returned an empty response.", isTransient: true);

            return content;
        }
    }
}
