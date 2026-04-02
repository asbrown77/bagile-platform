namespace Bagile.Infrastructure.Xero;

/// <summary>
/// Generic HTTP client for Xero API calls from the API layer.
/// Handles authentication, tenant header, and 401 retry automatically.
///
/// Note: The ETL layer uses the existing IXeroApiClient (Clients/IXeroApiClient.cs) which is
/// purpose-built for invoice collection. This interface is for new API-layer integrations
/// (e.g. invoice lookup, payment status checks from controllers).
/// </summary>
public interface IXeroHttpClient
{
    /// <summary>
    /// Performs a GET to the given Xero API path and deserialises the response.
    /// </summary>
    /// <param name="path">Path relative to https://api.xero.com, e.g. "/api.xro/2.0/Invoices/123"</param>
    Task<T> GetAsync<T>(string path, CancellationToken ct = default);

    /// <summary>
    /// Performs a POST to the given Xero API path with the given body and deserialises the response.
    /// </summary>
    /// <param name="path">Path relative to https://api.xero.com, e.g. "/api.xro/2.0/Invoices"</param>
    Task<T> PostAsync<T>(string path, object body, CancellationToken ct = default);
}
