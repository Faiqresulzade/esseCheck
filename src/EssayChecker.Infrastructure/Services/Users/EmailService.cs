using System.Net;
using System.Net.Mail;
using EssayChecker.Application.DTOs.Interfaces;
using EssayChecker.Application.Settings;
using Microsoft.Extensions.Options;

namespace EssayChecker.Infrastructure.Services.Users;

public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }

    public async Task SendAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            // Defolt 100 saniyəlik SmtpClient.Timeout istifadəçini çox uzun "loading" vəziyyətində
            // saxlayır (məs. hosting mühitində SMTP portu yavaşdırsa/bloklasa). 15 saniyə kifayətdir —
            // adi Gmail SMTP əlaqəsi bundan qat-qat tez qurulur.
            Timeout = 15000
        };

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        await client.SendMailAsync(message, linkedCts.Token);
    }
}
