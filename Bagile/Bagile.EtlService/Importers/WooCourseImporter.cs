using Bagile.Infrastructure.Models;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Projectors;

public class WooCourseImporter : IImporter<WooProductDto>
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

    public async Task ApplyAsync(WooProductDto product, CancellationToken ct = default)
    {
        var meta = product.MetaData?
            .Where(m => m.Key != null)
            .ToDictionary(
                m => m.Key!.ToLowerInvariant(),
                m => m.Value?.ToString() ?? string.Empty);

        DateTime? Parse(string? s) => DateTime.TryParse(s, out var d) ? d : null;

        var course = new CourseSchedule
        {
            Name = product.Name,
            Status = product.Status,
            StartDate = Parse(meta?.GetValueOrDefault("start_date")),
            EndDate = Parse(meta?.GetValueOrDefault("end_date")),
            Capacity = product.StockQuantity,
            Price = product.Price,
            Sku = product.Sku,
            TrainerName = meta?.GetValueOrDefault("trainer_name"),
            FormatType = meta?.GetValueOrDefault("format_type"),
            SourceSystem = "WooCommerce",
            SourceProductId = product.Id
        };

        _logger.LogInformation("Projecting WooCommerce product {Id} -> CourseSchedule", product.Id);

        await _schedules.UpsertAsync(course);
    }
}