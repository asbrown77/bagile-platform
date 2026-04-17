using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bagile.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bagile.Infrastructure.Services;

/// <summary>
/// Creates Scrum.org course listings by calling the bagile-pa HTTP service,
/// which runs a Playwright browser automation script.
///
/// Requires bagile-pa to be on bagile-net and reachable at PaService:BaseUrl.
/// </summary>
public class ScrumOrgPublishService : IScrumOrgPublishService
{
    private static readonly HttpClient _httpClient = new();

    private readonly ILogger<ScrumOrgPublishService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public ScrumOrgPublishService(
        IConfiguration config,
        ILogger<ScrumOrgPublishService> logger)
    {
        _logger = logger;
        _baseUrl = (config["PaService:BaseUrl"] ?? "http://bagile-pa:3001").TrimEnd('/');
        _apiKey = config["PaService:ApiKey"] ?? "";
    }

    public async Task<ScrumOrgPublishResult?> CreateListingAsync(
        ScrumOrgPublishRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating Scrum.org listing for {CourseType} on {StartDate} by {Trainer} via PA service",
            request.CourseType, request.StartDate.ToString("yyyy-MM-dd"), request.TrainerName);

        var payload = new
        {
            courseType = request.CourseType,
            trainerName = request.TrainerName,
            startDate = request.StartDate.ToString("yyyy-MM-dd"),
            endDate = request.EndDate.ToString("yyyy-MM-dd"),
            registrationUrl = request.RegistrationUrl,
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_baseUrl}/playwright/create-scrumorg-course")
        {
            Content = content
        };

        if (!string.IsNullOrWhiteSpace(_apiKey))
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "PA service returned {StatusCode} for Scrum.org publish: {Body}",
                    (int)response.StatusCode, responseBody);
                throw new InvalidOperationException(
                    $"PA service returned HTTP {(int)response.StatusCode}: {responseBody}");
            }

            var result = JsonSerializer.Deserialize<PaServiceResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Success != true || result.CourseUrl == null)
            {
                var reason = result?.ErrorMessage ?? "no courseUrl in response";
                _logger.LogError("PA service reported failure: {Error}", reason);
                throw new InvalidOperationException($"Scrum.org publish failed: {reason}");
            }

            _logger.LogInformation("Scrum.org listing created: {Url}", result.CourseUrl);

            return new ScrumOrgPublishResult
            {
                ListingUrl = result.CourseUrl
            };
        }
        catch (InvalidOperationException)
        {
            throw; // propagate meaningful errors to the command handler
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PA service for Scrum.org publish");
            throw new InvalidOperationException($"Failed to reach PA service: {ex.Message}", ex);
        }
    }

    private record PaServiceResponse
    {
        public bool Success { get; init; }
        public string? CourseUrl { get; init; }
        public string? ErrorMessage { get; init; }
        public int DurationMs { get; init; }
    }
}
