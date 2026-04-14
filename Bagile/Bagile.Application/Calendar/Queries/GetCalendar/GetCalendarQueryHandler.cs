using Bagile.Application.Calendar.DTOs;
using Bagile.Application.Common.Interfaces;
using MediatR;

namespace Bagile.Application.Calendar.Queries.GetCalendar;

public class GetCalendarQueryHandler
    : IRequestHandler<GetCalendarQuery, IEnumerable<CalendarEventDto>>
{
    private readonly ICalendarQueries _queries;

    public GetCalendarQueryHandler(ICalendarQueries queries)
    {
        _queries = queries;
    }

    public async Task<IEnumerable<CalendarEventDto>> Handle(
        GetCalendarQuery request,
        CancellationToken ct)
    {
        return await _queries.GetCalendarEventsAsync(
            request.From, request.To, request.TrainerId, ct);
    }
}
