using Bagile.Infrastructure.Models;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Projectors;

public class WooCourseImporter : IImporter<WooProduct>
{
    private readonly ICourseScheduleRepository _schedules;
    private readonly ICourseDefinitionRepository _definitions;
    private readonly ILogger<WooCourseImporter> _logger;

    public WooCourseImporter(
        ICourseScheduleRepository schedules,
        ICourseDefinitionRepository definitions,
        ILogger<WooCourseImporter> logger)
    {
        _schedules = schedules;
        _definitions = definitions;
        _logger = logger;
    }

    public async Task ApplyAsync(WooProduct product, CancellationToken ct = default)
    {
        var course = new CourseSchedule
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
            SourceSystem = "WooCommerce",
            SourceProductId = product.Id
        };

        _logger.LogInformation("Projecting WooCommerce product {Id} -> CourseSchedule", product.Id);

        await _schedules.UpsertAsync(course);
    }
}