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
}

// ── DTOs ─────────────────────────────────────────────────────

public record CourseDefResponse(int Id, string Code, string Name, int DurationDays, bool Active, string? BadgeUrl);

public record UpdateBadgeRequest
{
    public string? BadgeUrl { get; init; }
}
