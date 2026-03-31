using Bagile.Application.Analytics.Queries.GetCourseDemand;
using Bagile.Application.Analytics.Queries.GetOrganisationAnalytics;
using Bagile.Application.Analytics.Queries.GetPartnerAnalytics;
using Bagile.Application.Analytics.Queries.GetRepeatCustomers;
using Bagile.Application.Analytics.Queries.GetRevenueMonthDrilldown;
using Bagile.Application.Analytics.Queries.GetRevenueSummary;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Revenue ──────────────────────────────────────────────

    [HttpGet("revenue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueSummary(
        [FromQuery] int? year = null)
    {
        var result = await _mediator.Send(new GetRevenueSummaryQuery(year));
        return Ok(result);
    }

    [HttpGet("revenue/{year:int}/{month:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueMonthDrilldown(int year, int month)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { error = "Month must be between 1 and 12" });

        var result = await _mediator.Send(new GetRevenueMonthDrilldownQuery(year, month));
        return Ok(result);
    }

    // ── Organisations ────────────────────────────────────────

    [HttpGet("organisations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganisationAnalytics(
        [FromQuery] int? year = null,
        [FromQuery] string sortBy = "spend")
    {
        var result = await _mediator.Send(
            new GetOrganisationAnalyticsQuery(year, sortBy));
        return Ok(result);
    }

    [HttpGet("organisations/repeat-customers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepeatCustomers(
        [FromQuery] int? year = null,
        [FromQuery] int minBookings = 2)
    {
        var result = await _mediator.Send(
            new GetRepeatCustomersQuery(year, minBookings));
        return Ok(result);
    }

    // ── Partners ─────────────────────────────────────────────

    [HttpGet("partners")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPartnerAnalytics(
        [FromQuery] int? year = null)
    {
        var result = await _mediator.Send(new GetPartnerAnalyticsQuery(year));
        return Ok(result);
    }

    // ── Course Demand ────────────────────────────────────────

    [HttpGet("course-demand")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseDemand(
        [FromQuery] int months = 12)
    {
        var result = await _mediator.Send(new GetCourseDemandQuery(months));
        return Ok(result);
    }
}
