namespace Bagile.Application.Common.Interfaces;

/// <summary>
/// Sends HTML emails on behalf of the platform.
/// Implementations: SmtpEmailService (Infrastructure).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an HTML email.
    /// </summary>
    /// <param name="to">Primary recipients.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="htmlBody">Full HTML body.</param>
    /// <param name="cc">Optional CC recipients.</param>
    /// <param name="fromName">Display name for the From address (defaults to config value).</param>
    /// <param name="fromEmail">From address (defaults to config value).</param>
    Task SendAsync(
        IEnumerable<string> to,
        string subject,
        string htmlBody,
        IEnumerable<string>? cc = null,
        string? fromName = null,
        string? fromEmail = null,
        CancellationToken ct = default);
}
