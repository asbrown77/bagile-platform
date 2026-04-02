namespace Bagile.Infrastructure.Xero;

/// <summary>
/// Manages the Xero OAuth 2.0 token lifecycle for the API layer.
/// Automatically refreshes access tokens when they expire.
/// Token storage: bagile.integration_tokens (source = 'xero') — existing table from V11.
/// </summary>
public interface IXeroTokenManager
{
    /// <summary>
    /// Returns a valid Xero access token. Refreshes automatically if the current token
    /// has expired or is within 5 minutes of expiry.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no refresh token exists (Xero OAuth not initialised) or if the
    /// refresh token itself has expired and requires re-authorisation.
    /// </exception>
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// The Xero tenant ID for this organisation.
    /// </summary>
    string TenantId { get; }
}
