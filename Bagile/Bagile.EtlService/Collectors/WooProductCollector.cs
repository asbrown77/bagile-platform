using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Collectors;

public class WooProductCollector : IProductCollector
{
    private readonly IWooApiClient _wooApiClient;
    private readonly ICourseScheduleRepository _scheduleRepo;
    private readonly ICourseDefinitionRepository _definitionRepo;
    private readonly ILogger<WooProductCollector> _logger;

    public string SourceName => "WooCommerce";

    public WooProductCollector(
        IWooApiClient wooApiClient,
        ICourseScheduleRepository scheduleRepo,
        ICourseDefinitionRepository definitionRepo,
        ILogger<WooProductCollector> logger)
    {
        _wooApiClient = wooApiClient;
        _scheduleRepo = scheduleRepo;
        _definitionRepo = definitionRepo;
        _logger = logger;
    }

    public async Task CollectProductsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Collecting products (course schedules) from WooCommerce...");

        var products = await _wooApiClient.FetchProductsAsync(ct);

        if (products == null || products.Count == 0)
        {
            _logger.LogWarning("No products returned from WooCommerce.");
            return;
        }

        var definitions = (await _definitionRepo.GetAllAsync()).ToList();

        foreach (var product in products)
        {
            try
            {
                var matchedDef = MatchDefinition(product, definitions);

                var schedule = new CourseSchedule
                {
                    Name = product.Name,
                    Status = product.Status,
                    StartDate = product.Meta?.StartDate,
                    EndDate = product.Meta?.EndDate,
                    Capacity = product.StockQuantity,
                    Price = product.Price,
                    Sku = product.Sku,
                    TrainerName = product.Meta?.TrainerName,
                    FormatType = product.Meta?.FormatType,
                    CourseDefinitionId = matchedDef?.Id,
                    SourceSystem = "WooCommerce",
                    SourceProductId = product.Id,
                    LastSynced = DateTime.UtcNow
                };

                await _scheduleRepo.UpsertAsync(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process Woo product {ProductId}", product.Id);
            }
        }

        _logger.LogInformation("Processed {Count} WooCommerce products.", products.Count);
    }

    private static CourseDefinition? MatchDefinition(WooProduct product, List<CourseDefinition> definitions)
    {
        // Simple matching by category or product name
        // Adjust this logic once you know how Woo categories map
        return definitions.FirstOrDefault(d =>
            product.Name.Contains(d.Code, StringComparison.OrdinalIgnoreCase) ||
            product.Name.Contains(d.Name, StringComparison.OrdinalIgnoreCase));
    }
}
