using System.Net.Http.Json;
using System.Text.Json.Serialization;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Users;

/// <summary>
/// E-maili Brevo-nun HTTPS API-si ilə göndərir. SMTP-dən fərqli olaraq yalnız 443 portundan
/// istifadə edir, ona görə Render kimi çıxan SMTP-ni bloklayan platformalarda da işləyir.
/// </summary>
public sealed class BrevoEmailService : IEmailService
{
    private readonly HttpClient _http;
    private readonly EmailSettings _settings;

    public BrevoEmailService(HttpClient http, IOptions<EmailSettings> options)
    {
        _http = http;
        _settings = options.Value;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var payload = new BrevoEmailRequest
        {
            Sender = new BrevoContact { Email = _settings.SenderEmail, Name = _settings.SenderName },
            To = new List<BrevoContact> { new() { Email = to } },
            Subject = subject,
            HtmlContent = htmlBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.BrevoBaseUrl)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("api-key", _settings.BrevoApiKey);
        request.Headers.TryAddWithoutValidation("accept", "application/json");

        using var response = await _http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Brevo e-mail göndərmə xətası ({(int)response.StatusCode}): {body}");
        }
    }

    private sealed class BrevoEmailRequest
    {
        [JsonPropertyName("sender")]
        public BrevoContact Sender { get; set; } = null!;

        [JsonPropertyName("to")]
        public List<BrevoContact> To { get; set; } = new();

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = null!;

        [JsonPropertyName("htmlContent")]
        public string HtmlContent { get; set; } = null!;
    }

    private sealed class BrevoContact
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = null!;

        /// <summary>Alıcı üçün opsionaldır — null olduqda JSON-a əlavə edilmir.</summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }
    }
}
