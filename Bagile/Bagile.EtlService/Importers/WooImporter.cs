using System.Net.Http.Json;
using Bagile.Domain.Entities;
using Bagile.Infrastructure.Models;
using Bagile.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Importers;

public class WooImporter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WooImporter> _logger;

    public WooImporter(HttpClient httpClient, ILogger<WooImporter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<CourseSchedule>> ImportCoursesAsync(string url, CancellationToken ct)
    {
        _logger.LogInformation("Fetching WooCommerce products from {Url}", url);

        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var products = await response.Content.ReadFromJsonAsync<List<WooProductDto>>(cancellationToken: ct);

        if (products == null || products.Count == 0)
            return new List<CourseSchedule>();

        return products
            .Select(p => p.ToCourseSchedule())
            .ToList();
    }
}