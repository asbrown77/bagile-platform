using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Notifications
{
    public class EmailNotifier
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailNotifier> _logger;

        public EmailNotifier(IConfiguration config, ILogger<EmailNotifier> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string subject, string body)
        {
            var host = _config["Smtp:Host"];
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Pass"];
            var from = _config["Smtp:From"];
            var to = _config["Smtp:To"];

            // if nothing is configured, just skip quietly
            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(user) ||
                string.IsNullOrWhiteSpace(pass) ||
                string.IsNullOrWhiteSpace(from) ||
                string.IsNullOrWhiteSpace(to))
            {
                _logger.LogWarning("EmailNotifier: SMTP config missing, skipping email: {Subject}", subject);
                return;
            }

            try
            {
                using var client = new SmtpClient(host, 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(user, pass)
                };

                using var msg = new MailMessage(from, to, subject, body);
                await client.SendMailAsync(msg);

                _logger.LogInformation("EmailNotifier: sent alert email '{Subject}' to {To}", subject, to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailNotifier: failed to send email '{Subject}'", subject);
            }
        }
    }
}