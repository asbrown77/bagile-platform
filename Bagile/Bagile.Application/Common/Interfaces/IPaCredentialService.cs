namespace Bagile.Application.Common.Interfaces;

/// <summary>
/// Manages per-trainer credentials in the PA service (scrumorg_username,
/// scrumorg_password, scrumorg_session_cookies) and triggers Playwright login.
/// Implemented in Infrastructure layer via HTTP calls to bagile-pa.
/// </summary>
public interface IPaCredentialService
{
    /// <summary>
    /// Returns the credential status for a trainer's Scrum.org integration.
    /// </summary>
    Task<TrainerCredentialStatus> GetTrainerScrumOrgStatusAsync(int trainerId, CancellationToken ct = default);

    /// <summary>
    /// Sets a Scrum.org credential key (e.g. scrumorg_username or scrumorg_password)
    /// for a trainer in the PA service credential store.
    /// </summary>
    Task SetTrainerScrumOrgCredentialAsync(int trainerId, string key, string value, CancellationToken ct = default);

    /// <summary>
    /// Triggers a Playwright login to Scrum.org for the trainer, refreshing the
    /// stored session cookies. Long-running — Playwright can take up to 2 minutes.
    /// </summary>
    Task<ScrumOrgLoginResult> RefreshTrainerSessionAsync(int trainerId, CancellationToken ct = default);

    /// <summary>
    /// Verifies that stored session cookies can access /admin/courses/manage on Scrum.org.
    /// Navigates via Playwright but does not create or modify any data — dry-run check only.
    /// </summary>
    Task<ScrumOrgSessionVerifyResult> VerifyTrainerSessionAsync(int trainerId, CancellationToken ct = default);
}

/// <summary>
/// Summarises which Scrum.org credentials a trainer currently has stored.
/// </summary>
public record TrainerCredentialStatus(
    string? Username,
    bool HasPassword,
    bool HasCookies
);

/// <summary>
/// Result of a Playwright-driven Scrum.org login attempt.
/// </summary>
public record ScrumOrgLoginResult(
    bool Success,
    string? ErrorMessage
);

/// <summary>
/// Result of a session verification check against Scrum.org's course management page.
/// </summary>
public record ScrumOrgSessionVerifyResult(
    bool Accessible,
    string? CurrentUrl,
    string? ErrorMessage,
    int DurationMs
);
