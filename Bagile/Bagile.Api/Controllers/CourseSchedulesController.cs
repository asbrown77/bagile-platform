using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.CourseSchedules.Queries.GetCourseSchedules;
using Bagile.Application.CourseSchedules.Queries.GetCourseScheduleById;
using Bagile.Application.CourseSchedules.Queries.GetCourseAttendees;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/course-schedules")]
public class CourseSchedulesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CourseSchedulesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get course schedules with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseSchedules(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? courseCode = null,
        [FromQuery] string? trainer = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetCourseSchedulesQuery
        {
            From = from,
            To = to,
            CourseCode = courseCode,
            Trainer = trainer,
            Type = type,
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific course schedule by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCourseSchedule(long id)
    {
        var query = new GetCourseScheduleByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { error = $"Course schedule {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get attendees for a specific course schedule
    /// </summary>
    [HttpGet("{id}/attendees")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseAttendees(long id)
    {
        var query = new GetCourseAttendeesQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}