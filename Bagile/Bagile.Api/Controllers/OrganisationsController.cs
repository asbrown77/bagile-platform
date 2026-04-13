using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.Organisations.Queries.GetOrganisations;
using Bagile.Application.Organisations.Queries.GetOrganisationByName;
using Bagile.Application.Organisations.Queries.GetOrganisationCourseHistory;
using Bagile.Application.Organisations.Queries.SearchOrganisations;
using Bagile.Application.Organisations.Queries.GetOrgConfig;
using Bagile.Application.Organisations.Commands.CreateOrganisation;
using Bagile.Application.Organisations.Commands.UpdateOrgConfig;
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
    /// Type-ahead search against the organisations table (name + aliases).
    /// Returns up to 10 matching orgs with id, name, and acronym.
    /// Used by the CreatePrivateCourse / EditPrivateCourse panels.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchOrganisations([FromQuery] string q = "")
    {
        var result = await _mediator.Send(new SearchOrganisationsQuery { Q = q });
        return Ok(result);
    }

    /// <summary>
    /// Create a new organisation (from the portal type-ahead "Create as new org" flow).
    /// Returns the created org with its generated id.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrganisation([FromBody] CreateOrganisationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required" });

        try
        {
            var result = await _mediator.Send(new CreateOrganisationCommand
            {
                Name = request.Name.Trim(),
                Acronym = request.Acronym?.Trim(),
            });
            return CreatedAtAction(nameof(GetOrganisation), new { name = result.Name }, result);
        }
        catch (Exception ex) when (ex.Message.Contains("duplicate") || ex.Message.Contains("unique"))
        {
            return Conflict(new { error = $"Organisation '{request.Name}' already exists" });
        }
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
    public async Task<IActionResult> GetOrganisation(string name, [FromQuery] int? year = null)
    {
        // URL decode the name in case it contains encoded characters
        var decodedName = HttpUtility.UrlDecode(name);

        var query = new GetOrganisationByNameQuery(decodedName, year);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { error = $"Organisation '{decodedName}' not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get full configuration (aliases, primary domain) for an organisation.
    /// Returns 404 if the org is not in the organisations table.
    /// </summary>
    [HttpGet("{name}/config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrgConfig(string name)
    {
        var decodedName = HttpUtility.UrlDecode(name);
        var result = await _mediator.Send(new GetOrgConfigQuery(decodedName));
        if (result == null) return NotFound(new { error = "Organisation not found in organisations table" });
        return Ok(result);
    }

    /// <summary>
    /// Update aliases and primary domain for an organisation.
    /// </summary>
    [HttpPut("{name}/config")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrgConfig(string name, [FromBody] UpdateOrgConfigRequest request)
    {
        var decodedName = HttpUtility.UrlDecode(name);
        var existing = await _mediator.Send(new GetOrgConfigQuery(decodedName));
        if (existing == null) return NotFound(new { error = "Organisation not found in organisations table" });

        var result = await _mediator.Send(new UpdateOrgConfigCommand
        {
            Id            = existing.Id,
            Aliases       = request.Aliases,
            PrimaryDomain = request.PrimaryDomain?.Trim(),
        });
        return Ok(result);
    }

    /// <summary>
    /// Get course history for a specific organisation
    /// </summary>
    [HttpGet("{name}/course-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrganisationCourseHistory(string name, [FromQuery] int? year = null)
    {
        // URL decode the name in case it contains encoded characters
        var decodedName = HttpUtility.UrlDecode(name);

        var query = new GetOrganisationCourseHistoryQuery(decodedName, year);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

public record CreateOrganisationRequest
{
    public string Name { get; init; } = "";
    public string? Acronym { get; init; }
}

public record UpdateOrgConfigRequest
{
    public List<string> Aliases { get; init; } = new();
    public string? PrimaryDomain { get; init; }
}