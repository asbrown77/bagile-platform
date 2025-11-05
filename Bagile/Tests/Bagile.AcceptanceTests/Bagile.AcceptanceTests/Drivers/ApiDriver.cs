using System.Net.Http.Json;
using System.Text.Json;
using Bagile.Application.Common.Models;

namespace Bagile.AcceptanceTests.Drivers;

/// <summary>
/// Wrapper around HttpClient for making API requests in tests.
/// </summary>
public class ApiDriver
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _lastResponse;
    private string? _lastContent;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiDriver(HttpClient client)
    {
        _client = client;
    }

    public int LastResponseStatus => (int)(_lastResponse?.StatusCode ?? 0);
    public string LastResponseContent => _lastContent ?? string.Empty;

    public async Task GetOrdersAsync(
        string? status = null,
        DateTime? from = null,
        DateTime? to = null,
        string? email = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = $"?page={page}&pageSize={pageSize}";
        if (status != null) query += $"&status={status}";
        if (from != null) query += $"&from={from:yyyy-MM-dd}";
        if (to != null) query += $"&to={to:yyyy-MM-dd}";
        if (email != null) query += $"&email={email}";

        _lastResponse = await _client.GetAsync($"/api/orders{query}");
        _lastContent = await _lastResponse.Content.ReadAsStringAsync();
    }

    public async Task GetOrderByIdAsync(long orderId)
    {
        _lastResponse = await _client.GetAsync($"/api/orders/{orderId}");
        _lastContent = await _lastResponse.Content.ReadAsStringAsync();
    }

    public PagedResult<T> GetLastPagedResult<T>()
    {
        EnsureContent();
        return JsonSerializer.Deserialize<PagedResult<T>>(_lastContent!, JsonOpts)!;
    }

    public T GetLastSingleResult<T>()
    {
        EnsureContent();
        return JsonSerializer.Deserialize<T>(_lastContent!, JsonOpts)!;
    }

    private void EnsureContent()
    {
        if (_lastResponse == null)
            throw new InvalidOperationException("No HTTP response recorded yet.");
        if (_lastContent == null)
            throw new InvalidOperationException("Response content has not been read.");
    }
}
