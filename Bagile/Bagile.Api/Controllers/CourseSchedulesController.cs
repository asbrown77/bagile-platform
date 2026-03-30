using System.Text;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.CourseSchedules.Queries.GetCourseSchedules;
using Bagile.Application.CourseSchedules.Queries.GetCourseScheduleById;
using Bagile.Application.CourseSchedules.Queries.GetCourseAttendees;
using Bagile.Application.CourseSchedules.Queries.GetCourseMonitoring;
using Bagile.Application.CourseSchedules.Commands.CancelCourse;

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

    /// <summary>
    /// Export attendees as CSV
    /// </summary>
    [HttpGet("{id}/attendees/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCourseAttendees(long id)
    {
        var query = new GetCourseAttendeesQuery(id);
        var attendees = (await _mediator.Send(query)).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("First Name,Last Name,Email,Organisation,Status,Course Code,Course Name");
        foreach (var a in attendees)
            sb.AppendLine($"{Csv(a.FirstName)},{Csv(a.LastName)},{Csv(a.Email)},{Csv(a.Organisation)},{Csv(a.Status)},{Csv(a.CourseCode)},{Csv(a.CourseName)}");

        var sku = attendees.FirstOrDefault()?.CourseCode ?? id.ToString();
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"attendees-{sku}.csv");
    }

    private static string Csv(string? v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        return v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
    }

    /// <summary>
    /// Get course monitoring data — enrolment vs minimums, decision deadlines, recommended actions
    /// </summary>
    [HttpGet("monitoring")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseMonitoring([FromQuery] int daysAhead = 30)
    {
        var query = new GetCourseMonitoringQuery { DaysAhead = daysAhead };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Cancel a course schedule
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelCourse(long id, [FromBody] CancelCourseRequest? request = null)
    {
        var command = new CancelCourseCommand { CourseScheduleId = id, Reason = request?.Reason ?? "" };
        var result = await _mediator.Send(command);

        if (result == null)
            return NotFound(new { error = $"Course schedule {id} not found" });

        return Ok(result);
    }
}

public record CancelCourseRequest
{
    public string Reason { get; init; } = "";
}