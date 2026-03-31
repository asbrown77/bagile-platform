using Bagile.Application.Dashboard.Queries.GetDashboardOverview;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("overview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview()
    {
        var result = await _mediator.Send(new GetDashboardOverviewQuery());
        return Ok(result);
    }
}
