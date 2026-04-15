using Bagile.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/course-definitions")]
public class CourseDefinitionsController : ControllerBase
{
    private readonly ICourseDefinitionRepository _repo;

    public CourseDefinitionsController(ICourseDefinitionRepository repo) => _repo = repo;

    /// <summary>List all course definitions with badge URLs.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var defs = await _repo.GetAllAsync();
        var result = defs.Select(d => new CourseDefResponse(
            d.Id, d.Code, d.Name, d.DurationDays, d.Active, d.BadgeUrl));
        return Ok(result);
    }

    /// <summary>Update the duration (days) for a course definition.</summary>
    [HttpPatch("{code}/duration")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDuration(string code, [FromBody] UpdateDurationRequest request, CancellationToken ct)
    {
        if (request.DurationDays < 1 || request.DurationDays > 10)
            return BadRequest(new { error = "durationDays must be between 1 and 10" });

        var existing = await _repo.GetByCodeAsync(code);
        if (existing is null)
            return NotFound(new { error = $"Course definition '{code}' not found" });

        await _repo.UpdateDurationAsync(code, request.DurationDays);
        return NoContent();
    }

    /// <summary>Update the badge URL for a course definition.</summary>
    [HttpPatch("{code}/badge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBadge(string code, [FromBody] UpdateBadgeRequest request, CancellationToken ct)
    {
        var existing = await _repo.GetByCodeAsync(code);
        if (existing is null)
            return NotFound(new { error = $"Course definition '{code}' not found" });

        await _repo.UpdateBadgeUrlAsync(code, string.IsNullOrWhiteSpace(request.BadgeUrl) ? null : request.BadgeUrl);
        return NoContent();
    }

    /// <summary>Update the name for a course definition.</summary>
    [HttpPatch("{code}/name")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateName(string code, [FromBody] UpdateNameRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "name must not be empty" });

        var existing = await _repo.GetByCodeAsync(code);
        if (existing is null)
            return NotFound(new { error = $"Course definition '{code}' not found" });

        await _repo.UpdateNameAsync(code, request.Name.Trim());
        return NoContent();
    }

    /// <summary>Create a new course definition.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCourseDefRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "code and name must not be empty" });

        if (request.DurationDays < 1 || request.DurationDays > 10)
            return BadRequest(new { error = "durationDays must be between 1 and 10" });

        var existing = await _repo.GetByCodeAsync(request.Code.Trim().ToUpperInvariant());
        if (existing is not null)
            return Conflict(new { error = $"Course definition '{request.Code}' already exists" });

        var created = await _repo.CreateAsync(request.Code.Trim().ToUpperInvariant(), request.Name.Trim(), request.DurationDays);
        var response = new CourseDefResponse(created.Id, created.Code, created.Name, created.DurationDays, created.Active, created.BadgeUrl);
        return CreatedAtAction(nameof(GetAll), response);
    }

    /// <summary>List aliases for a course definition.</summary>
    [HttpGet("{code}/aliases")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAliases(string code, CancellationToken ct)
    {
        var existing = await _repo.GetByCodeAsync(code);
        if (existing is null)
            return NotFound(new { error = $"Course definition '{code}' not found" });

        var aliases = await _repo.GetAliasesAsync(code);
        return Ok(aliases);
    }

    /// <summary>Add an alias to a course definition.</summary>
    [HttpPost("{code}/aliases")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAlias(string code, [FromBody] AddAliasRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Alias))
            return BadRequest(new { error = "alias must not be empty" });

        var existing = await _repo.GetByCodeAsync(code);
        if (existing is null)
            return NotFound(new { error = $"Course definition '{code}' not found" });

        if (await _repo.AliasExistsAsync(request.Alias.Trim()))
            return BadRequest(new { error = $"Alias '{request.Alias}' already exists" });

        await _repo.AddAliasAsync(code, request.Alias.Trim());
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>Remove an alias from a course definition.</summary>
    [HttpDelete("{code}/aliases/{alias}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAlias(string code, string alias, CancellationToken ct)
    {
        var removed = await _repo.RemoveAliasAsync(code, alias);
        if (!removed)
            return NotFound(new { error = $"Alias '{alias}' not found for course '{code}'" });

        return NoContent();
    }
}

// ── DTOs ─────────────────────────────────────────────────────

public record CourseDefResponse(int Id, string Code, string Name, int DurationDays, bool Active, string? BadgeUrl);

public record UpdateBadgeRequest
{
    public string? BadgeUrl { get; init; }
}

public record UpdateDurationRequest
{
    public int DurationDays { get; init; }
}

public record UpdateNameRequest
{
    public string Name { get; init; } = "";
}

public record CreateCourseDefRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public int DurationDays { get; init; }
}

public record AddAliasRequest
{
    public string Alias { get; init; } = "";
}
