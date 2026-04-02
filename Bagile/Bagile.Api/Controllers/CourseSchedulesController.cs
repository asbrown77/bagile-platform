using System.Text;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.CourseSchedules.Queries.GetCourseSchedules;
using Bagile.Application.CourseSchedules.Queries.GetCourseScheduleById;
using Bagile.Application.CourseSchedules.Queries.GetCourseAttendees;
using Bagile.Application.CourseSchedules.Queries.GetCourseMonitoring;
using Bagile.Application.CourseSchedules.Queries.GetScheduleConflicts;
using Bagile.Application.CourseSchedules.Commands.CancelCourse;
using Bagile.Application.CourseSchedules.Commands.CreatePrivateCourse;
using Bagile.Application.CourseSchedules.Commands.AddPrivateAttendees;
using Bagile.Application.CourseSchedules.Commands.UpdatePrivateCourse;
using Bagile.Application.CourseSchedules.Commands.RemovePrivateAttendee;
using Bagile.Application.CourseSchedules.Commands.ManageCourseContacts;
using Bagile.Application.Templates.Queries;

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
        sb.AppendLine("First Name,Last Name,Email,Country");
        foreach (var a in attendees)
            sb.AppendLine($"{Csv(a.FirstName)},{Csv(a.LastName)},{Csv(a.Email)},{Csv(a.Country)}");

        // Filename: PSPO-Students-300326.csv
        var sku = attendees.FirstOrDefault()?.CourseCode ?? id.ToString();
        var code = sku.Split('-')[0];
        var datePart = sku.Contains('-') && sku.Split('-').Length > 1 ? sku.Split('-')[1] : id.ToString();
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"{code}-Students-{datePart}.csv");
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
    // ── Private Courses ────────────────────────────────────

    [HttpPost("private")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePrivateCourse(
        [FromBody] CreatePrivateCourseCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCourseSchedule),
            new { id = result.Id }, result);
    }

    [HttpPost("{id}/attendees")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPrivateAttendees(
        long id,
        [FromBody] AddPrivateAttendeesRequest request)
    {
        var command = new AddPrivateAttendeesCommand
        {
            CourseScheduleId = id,
            Attendees = request.Attendees
        };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Update mutable fields of a private course.
    /// Returns 404 if the course does not exist or is a public course.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrivateCourse(
        long id,
        [FromBody] UpdatePrivateCourseCommand request)
    {
        var command = request with { Id = id };
        var result = await _mediator.Send(command);

        if (result == null)
            return NotFound(new { error = $"Private course {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Remove (cancel) an attendee from a private course.
    /// Returns 404 if the enrolment doesn't exist, is already cancelled,
    /// or doesn't belong to this private course.
    /// </summary>
    [HttpDelete("{id}/attendees/{enrolmentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePrivateAttendee(long id, long enrolmentId)
    {
        var command = new RemovePrivateAttendeeCommand(id, enrolmentId);
        var removed = await _mediator.Send(command);

        if (!removed)
            return NotFound(new { error = $"Active enrolment {enrolmentId} not found on private course {id}" });

        return NoContent();
    }

    [HttpPost("parse-attendees")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ParseAttendees(
        [FromBody] ParseAttendeesRequest request)
    {
        var result = await _mediator.Send(new ParseAttendeesCommand(request.RawText));
        return Ok(result);
    }

    // ── Course Contacts ──────────────────────────────────

    /// <summary>
    /// Get contacts for a course (admin, organiser, other).
    /// Intended for private courses.
    /// </summary>
    [HttpGet("{id}/contacts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseContacts(long id)
    {
        var result = await _mediator.Send(new GetCourseContactsQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Add a contact to a course.
    /// </summary>
    [HttpPost("{id}/contacts")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddCourseContact(
        long id,
        [FromBody] AddCourseContactRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Name and email are required" });

        var validRoles = new[] { "admin", "organiser", "other" };
        var role = request.Role?.ToLowerInvariant() ?? "other";
        if (!validRoles.Contains(role))
            return BadRequest(new { error = $"Role must be one of: {string.Join(", ", validRoles)}" });

        var contact = await _mediator.Send(new AddCourseContactCommand(
            id, role, request.Name.Trim(), request.Email.Trim(), request.Phone?.Trim()));

        return CreatedAtAction(nameof(GetCourseContacts), new { id }, contact);
    }

    /// <summary>
    /// Update a contact on a course.
    /// </summary>
    [HttpPut("{id}/contacts/{contactId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCourseContact(
        long id,
        long contactId,
        [FromBody] UpdateCourseContactRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Name and email are required" });

        var validRoles = new[] { "admin", "organiser", "other" };
        var role = request.Role?.ToLowerInvariant() ?? "other";
        if (!validRoles.Contains(role))
            return BadRequest(new { error = $"Role must be one of: {string.Join(", ", validRoles)}" });

        var result = await _mediator.Send(new UpdateCourseContactCommand(
            id, contactId, role, request.Name.Trim(), request.Email.Trim(), request.Phone?.Trim()));

        if (result is null)
            return NotFound(new { error = $"Contact {contactId} not found on course {id}" });

        return Ok(result);
    }

    /// <summary>
    /// Delete a contact from a course.
    /// </summary>
    [HttpDelete("{id}/contacts/{contactId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourseContact(long id, long contactId)
    {
        var deleted = await _mediator.Send(new DeleteCourseContactCommand(id, contactId));
        if (!deleted)
            return NotFound(new { error = $"Contact {contactId} not found on course {id}" });

        return NoContent();
    }

    // ── Email send log ───────────────────────────────────

    /// <summary>
    /// Get the email send history for a course schedule.
    /// Includes both pre-course and post-course sends, test and real.
    /// </summary>
    [HttpGet("{id}/email-log")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmailLog(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEmailSendLogQuery((int)id), ct);
        return Ok(result);
    }

    // ── Conflicts ────────────────────────────────────────

    [HttpGet("conflicts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduleConflicts(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? trainer = null)
    {
        var result = await _mediator.Send(
            new GetScheduleConflictsQuery(startDate, endDate, trainer));
        return Ok(result);
    }
}

public record CancelCourseRequest
{
    public string Reason { get; init; } = "";
}

public record AddPrivateAttendeesRequest
{
    public List<AttendeeInput> Attendees { get; init; } = new();
}

public record ParseAttendeesRequest
{
    public string RawText { get; init; } = "";
}

public record AddCourseContactRequest
{
    public string? Role { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
}

public record UpdateCourseContactRequest
{
    public string? Role { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
}