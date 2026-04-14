using Bagile.Application.Calendar.DTOs;
using MediatR;

namespace Bagile.Application.Calendar.Queries.GetCalendar;

public record GetCalendarQuery : IRequest<IEnumerable<CalendarEventDto>>
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public int? TrainerId { get; init; }
}
