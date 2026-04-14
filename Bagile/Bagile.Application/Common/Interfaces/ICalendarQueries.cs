using Bagile.Application.Calendar.DTOs;

namespace Bagile.Application.Common.Interfaces;

public interface ICalendarQueries
{
    /// <summary>
    /// Returns a unified calendar feed combining planned courses and
    /// live course schedules, enriched with gateway publication status.
    /// </summary>
    Task<IEnumerable<CalendarEventDto>> GetCalendarEventsAsync(
        DateTime from,
        DateTime to,
        int? trainerId,
        CancellationToken ct = default);
}
