using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.Enrolments.Queries.GetEnrolments;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnrolmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get enrolments with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrolments(
        [FromQuery] long? courseScheduleId = null,
        [FromQuery] long? studentId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? organisation = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetEnrolmentsQuery
        {
            CourseScheduleId = courseScheduleId,
            StudentId = studentId,
            Status = status,
            Organisation = organisation,
            From = from,
            To = to,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }
}