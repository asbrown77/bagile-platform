using System.Net.Http.Json;
using Bagile.Infrastructure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bagile.Infrastructure.Clients;

public class FooEventsTicketsClient : IFooEventsTicketsClient
{
    private readonly HttpClient _http;
    private readonly ILogger<FooEventsTicketsClient> _logger;

    public FooEventsTicketsClient(
        HttpClient http,
        IConfiguration config,
        ILogger<FooEventsTicketsClient> logger)
    {
        _http = http;
        _logger = logger;

        var baseUrl = config["Bagile:BaseUrl"]
            ?? throw new InvalidOperationException("Bagile:BaseUrl not configured");
        var apiKey = config["Bagile:FooEventsApiKey"]
            ?? throw new InvalidOperationException("Bagile:FooEventsApiKey not configured");

        _http.BaseAddress = new Uri(baseUrl);
        _http.DefaultRequestHeaders.Add("X-Bagile-Key", apiKey);
    }

    public async Task<IReadOnlyList<FooEventTicketDto>> FetchTicketsForOrderAsync(
        int orderId,
        CancellationToken ct = default)
    {
        var url = $"/wp-json/bagile/v1/orders/{orderId}/tickets";
        _logger.LogInformation("Fetching FooEvents tickets for order {OrderId}", orderId);

        try
        {
            var response = await _http.GetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No tickets found for order {OrderId}", orderId);
                return Array.Empty<FooEventTicketDto>();
            }

            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<FooEventsTicketsResponse>(
                cancellationToken: ct);

            if (payload == null)
            {
                _logger.LogWarning(
                    "FooEvents API returned null payload for order {OrderId}", orderId);
                return Array.Empty<FooEventTicketDto>();
            }

            var tickets = payload.Tickets ?? new List<FooEventTicketDto>();

            _logger.LogInformation(
                "Fetched {Count} tickets for order {OrderId} (currency {Currency})",
                tickets.Count,
                orderId,
                payload.Currency);

            return tickets;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching tickets for order {OrderId}", orderId);
            return Array.Empty<FooEventTicketDto>();
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Unsupported content type from FooEvents for order {OrderId}", orderId);
            return Array.Empty<FooEventTicketDto>();
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "JSON error deserializing FooEvents tickets for order {OrderId}", orderId);
            return Array.Empty<FooEventTicketDto>();
        }
    }
}
