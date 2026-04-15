using System.Text.Json;
using System.Text.Json.Serialization;
using Bagile.Application.Common.Interfaces;

namespace Bagile.Api.Endpoints;

public static class PublicEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void MapPublicEndpoints(this WebApplication app)
    {
        app.MapGet("/api/public/schedule", GetSchedule)
           .ExcludeFromDescription();
    }

    private static async Task<IResult> GetSchedule(
        HttpContext context,
        ICalendarQueries calendarQueries,
        CancellationToken ct)
    {
        // Allow any origin — this is intentionally public data
        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Cache-Control"] = "public, max-age=300";

        var from = DateTime.UtcNow.Date;
        var to = from.AddMonths(12);

        var events = await calendarQueries.GetCalendarEventsAsync(from, to, trainerId: null, ct);

        var schedule = events
            .Where(e => e.Status == "live" && !e.IsPrivate)
            .OrderBy(e => e.StartDate)
            .Select(e => new PublicCourseDto
            {
                Id = e.Id,
                CourseType = e.CourseType,
                StartDate = e.StartDate.ToString("yyyy-MM-dd"),
                EndDate = e.EndDate.ToString("yyyy-MM-dd"),
                TrainerName = e.TrainerName,
                IsVirtual = e.IsVirtual,
                Venue = e.Venue,
                EcommerceUrl = e.Gateways.FirstOrDefault(g => g.Type == "ecommerce")?.Url,
                ScrumorgUrl = e.Gateways.FirstOrDefault(g => g.Type == "scrumorg")?.Url
            })
            .ToList();

        return Results.Json(schedule, JsonOptions);
    }

    private sealed record PublicCourseDto
    {
        public string Id { get; init; } = "";
        public string CourseType { get; init; } = "";
        public string StartDate { get; init; } = "";
        public string EndDate { get; init; } = "";
        public string? TrainerName { get; init; }
        public bool IsVirtual { get; init; }
        public string? Venue { get; init; }
        public string? EcommerceUrl { get; init; }
        public string? ScrumorgUrl { get; init; }
    }
}
