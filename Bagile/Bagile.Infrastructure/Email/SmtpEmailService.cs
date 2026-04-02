using System.Net;
using System.Net.Mail;
using Bagile.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bagile.Infrastructure.Email;

/// <summary>
/// SMTP-backed email service. Uses the Smtp:* config section shared with EtlService.
/// For MVP — replace with SendGrid/SES if volume grows.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(
        IEnumerable<string> to,
        string subject,
        string htmlBody,
        IEnumerable<string>? cc = null,
        string? fromName = null,
        string? fromEmail = null,
        CancellationToken ct = default)
    {
        var host     = _config["Smtp:Host"];
        var user     = _config["Smtp:User"];
        var pass     = _config["Smtp:Pass"];
        var cfgFrom  = _config["Smtp:From"] ?? "noreply@bagile.co.uk";
        var portStr  = _config["Smtp:Port"] ?? "587";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            _logger.LogWarning("SmtpEmailService: SMTP config missing — email not sent: {Subject}", subject);
            return;
        }

        var effectiveFrom  = fromEmail ?? cfgFrom;
        var effectiveName  = fromName ?? "b-agile";
        var toList         = to.ToList();
        var ccList         = cc?.ToList() ?? [];

        if (toList.Count == 0)
        {
            _logger.LogWarning("SmtpEmailService: no recipients for email '{Subject}' — skipping", subject);
            return;
        }

        try
        {
            var port = int.TryParse(portStr, out var p) ? p : 587;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl  = true,
                Credentials = new NetworkCredential(user, pass)
            };

            using var msg = new MailMessage
            {
                From    = new MailAddress(effectiveFrom, effectiveName),
                Subject = subject,
                Body    = htmlBody,
                IsBodyHtml = true
            };

            foreach (var addr in toList)
                msg.To.Add(addr);

            foreach (var addr in ccList)
                msg.CC.Add(addr);

            await client.SendMailAsync(msg, ct);

            _logger.LogInformation(
                "SmtpEmailService: sent '{Subject}' to {Count} recipient(s)",
                subject, toList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmtpEmailService: failed to send '{Subject}'", subject);
            throw; // surface to caller so API can return 500 rather than silently losing the send
        }
    }
}
