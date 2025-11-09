using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.Organisations.Queries.GetOrganisations;
using Bagile.Application.Organisations.Queries.GetOrganisationByName;
using Bagile.Application.Organisations.Queries.GetOrganisationCourseHistory;
using System.Web;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/organisations")]
public class OrganisationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganisationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get organisations with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganisations(
        [FromQuery] string? name = null,
        [FromQuery] string? domain = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetOrganisationsQuery
        {
            Name = name,
            Domain = domain,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific organisation by name
    /// Note: Use URL encoding for organisation names with spaces or special characters
    /// </summary>
    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrganisation(string name)
    {
        // URL decode the name in case it contains encoded characters
        var decodedName = HttpUtility.UrlDecode(name);

        var query = new GetOrganisationByNameQuery(decodedName);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { error = $"Organisation '{decodedName}' not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get course history for a specific organisation
    /// </summary>
    [HttpGet("{name}/course-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganisationCourseHistory(string name)
    {
        // URL decode the name in case it contains encoded characters
        var decodedName = HttpUtility.UrlDecode(name);

        var query = new GetOrganisationCourseHistoryQuery(decodedName);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}