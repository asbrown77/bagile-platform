using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.Transfers.Queries.GetTransfers;
using Bagile.Application.Transfers.Queries.GetPendingTransfers;
using Bagile.Application.Transfers.Queries.GetTransfersByCourse;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/transfers")]
public class TransfersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransfersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get transfers with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransfers(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? reason = null,
        [FromQuery] string? organisationName = null,
        [FromQuery] long? courseScheduleId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetTransfersQuery
        {
            From = from,
            To = to,
            Reason = reason,
            OrganisationName = organisationName,
            CourseScheduleId = courseScheduleId,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get pending transfers (students cancelled but not rebooked)
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingTransfers()
    {
        var query = new GetPendingTransfersQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get transfers for a specific course schedule (both in and out)
    /// </summary>
    [HttpGet("by-course/{scheduleId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransfersByCourse(long scheduleId)
    {
        var query = new GetTransfersByCourseQuery(scheduleId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}