using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.Students.Queries.GetStudents;
using Bagile.Application.Students.Queries.GetStudentById;
using Bagile.Application.Students.Queries.GetStudentEnrolments;

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
}