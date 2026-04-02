using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.Students.Queries.GetStudents;
using Bagile.Application.Students.Queries.GetStudentById;
using Bagile.Application.Students.Queries.GetStudentEnrolments;
using Bagile.Application.Students.Commands.UpdateStudent;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get students with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudents(
        [FromQuery] string? email = null,
        [FromQuery] string? name = null,
        [FromQuery] string? organisation = null,
        [FromQuery] string? courseCode = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetStudentsQuery
        {
            Email = email,
            Name = name,
            Organisation = organisation,
            CourseCode = courseCode,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific student by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudent(long id)
    {
        var query = new GetStudentByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { error = $"Student {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get enrolment timeline for a specific student
    /// </summary>
    [HttpGet("{id}/enrolments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentEnrolments(long id)
    {
        var query = new GetStudentEnrolmentsQuery(id);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Manually override student details (email, name, company).
    /// Overridden fields survive ETL re-syncs — the ETL will not overwrite them.
    /// Use this for PTN/partner orders where the registered email belongs to the partner,
    /// not the actual attendee. Portal-only change — no effect on FooEvents tickets.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStudent(long id, [FromBody] UpdateStudentRequest request)
    {
        if (request.Email is null && request.FirstName is null &&
            request.LastName is null && request.Company is null)
        {
            return BadRequest(new { error = "Provide at least one field to update (email, firstName, lastName, or company)" });
        }

        var command = new UpdateStudentCommand
        {
            Id           = id,
            Email        = request.Email,
            FirstName    = request.FirstName,
            LastName     = request.LastName,
            Company      = request.Company,
            UpdatedBy    = request.UpdatedBy,
            OverrideNote = request.OverrideNote
        };

        var result = await _mediator.Send(command);
        if (result is null)
            return NotFound(new { error = $"Student {id} not found" });

        return Ok(result);
    }
}

// ── Request models ──────────────────────────────────────────

public record UpdateStudentRequest
{
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Company { get; init; }
    public string? UpdatedBy { get; init; }
    public string? OverrideNote { get; init; }
}