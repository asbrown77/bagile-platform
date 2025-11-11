using Bagile.Domain.Entities;
using Bagile.Infrastructure.Helpers;
using Bagile.Infrastructure.Models;
using System.Globalization;

namespace Bagile.Infrastructure.Mappers;

public static class WooMapper
{
    public static CourseSchedule ToCourseSchedule(this WooProductDto source)
    {
        var meta = source.MetaData?
            .Where(m => !string.IsNullOrWhiteSpace(m.Key))
            .GroupBy(m => m.Key!.Trim().ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Value?.ToString()?.Trim())
                      .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty
            ) ?? new Dictionary<string, string>();

        DateTime? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateTime.TryParse(s, out var d)) return d;
            if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
            return null;
        }

        var startDate = ParseDate(meta.GetValueOrDefault("woocommerceeventsdate")
                         ?? meta.GetValueOrDefault("event_date")
                         ?? meta.GetValueOrDefault("start_date"));

        var endDate = ParseDate(meta.GetValueOrDefault("woocommerceeventsenddate")
                       ?? meta.GetValueOrDefault("event_end_date")
                       ?? meta.GetValueOrDefault("end_date"))
                       ?? startDate;

        var formatType = meta.GetValueOrDefault("event_type")
                         ?? meta.GetValueOrDefault("format_type")
                         ?? meta.GetValueOrDefault("woocommerceeventslocation");

        // 🧩 trainer logic
        var trainer = meta.GetValueOrDefault("trainer_name")
                       ?? TrainerLookup.FromSku(source.Sku);

        return new CourseSchedule
        {
            SourceProductId = source.Id,
            Name = source.Name,
            Status = source.Status,
            Price = source.Price,
            Sku = source.Sku,
            TrainerName = trainer,
            FormatType = formatType,
            StartDate = startDate,
            EndDate = endDate,
            SourceSystem = "woo"
        };
    }
}
