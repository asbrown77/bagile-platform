using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.Calendar.Queries.GetCalendar;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly IMediator _mediator;

    public CalendarController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Unified calendar feed combining planned courses and live course schedules,
    /// enriched with gateway publication status per course.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? trainerId = null)
    {
        // Default to current month if no range specified
        var fromDate = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var toDate = to ?? fromDate.AddMonths(1).AddDays(-1);

        if (toDate < fromDate)
            return BadRequest(new { error = "to must be on or after from" });

        var query = new GetCalendarQuery
        {
            From = fromDate,
            To = toDate,
            TrainerId = trainerId
        };

        var result = (await _mediator.Send(query)).ToList();
        return Ok(result);
    }
}
