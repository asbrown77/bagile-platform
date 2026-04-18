using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.PlannedCourses.Commands.CreatePlannedCourse;
using Bagile.Application.PlannedCourses.Commands.BulkCreatePlannedCourses;
using Bagile.Application.PlannedCourses.Commands.UpdatePlannedCourse;
using Bagile.Application.PlannedCourses.Commands.DeletePlannedCourse;
using Bagile.Application.PlannedCourses.Commands.PublishEcommerce;
using Bagile.Application.PlannedCourses.Commands.PublishScrumOrg;
using Bagile.Application.PlannedCourses.Queries.GetPlannedCourses;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/planned-courses")]
public class PlannedCoursesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWooCommercePublishService _woo;
    private readonly IPlannedCourseQueries _plannedCourseQueries;

    public PlannedCoursesController(
        IMediator mediator,
        IWooCommercePublishService woo,
        IPlannedCourseQueries plannedCourseQueries)
    {
        _mediator = mediator;
        _woo = woo;
        _plannedCourseQueries = plannedCourseQueries;
    }

    /// <summary>
    /// List all planned courses, ordered by start_date ascending.
    /// Returns an array of PlannedCourseDto (includes trainer name).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPlannedCoursesQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Create a new planned course (portal-only scheduling intent).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePlannedCourseCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.CourseType))
            return BadRequest(new { error = "courseType is required" });
        if (command.TrainerId <= 0)
            return BadRequest(new { error = "trainerId is required" });
        if (command.StartDate == default)
            return BadRequest(new { error = "startDate is required" });
        if (command.EndDate == default)
            return BadRequest(new { error = "endDate is required" });
        if (command.EndDate < command.StartDate)
            return BadRequest(new { error = "endDate must be on or after startDate" });

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    /// <summary>
    /// Bulk-create planned courses from an array. Validates each row individually.
    /// Partial imports are allowed — failed rows return an error, successful rows return an id.
    /// Returns per-row results: { index, success, id?, error? }.
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreatePlannedCoursesCommand command)
    {
        if (command.Courses == null || command.Courses.Count == 0)
            return BadRequest(new { error = "courses array is required and must not be empty" });
        if (command.Courses.Count > 200)
            return BadRequest(new { error = "Maximum 200 courses per bulk request" });

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Update a planned course. Only provided fields are changed (PATCH semantics).
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePlannedCourseCommand command)
    {
        var merged = command with { Id = id };
        var result = await _mediator.Send(merged);

        if (result == null)
            return NotFound(new { error = $"Planned course {id} not found" });

        return Ok(result);
    }

    /// <summary>
    /// Delete a planned course. Blocked if any gateway publications exist (409).
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeletePlannedCourseCommand(id));

        if (!result.Found)
            return NotFound(new { error = $"Planned course {id} not found" });

        if (result.HasPublications)
            return Conflict(new { error = "Cannot delete a course that has been published to one or more gateways. Cancel it instead." });

        return NoContent();
    }

    /// <summary>
    /// Publish a planned course to the E-commerce gateway (WooCommerce).
    /// Creates a draft WooCommerce product with correct dates, SKU, trainer, and FooEvents meta.
    /// </summary>
    [HttpPost("{id}/publish/ecommerce")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishEcommerce(int id)
    {
        try
        {
            var result = await _mediator.Send(new PublishEcommerceCommand(id));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Returns the WooCommerce product SKU that would be used as the template
    /// when the user clicks "Create in shop". Shows this as a preview in the UI
    /// before the user commits to creation.
    /// Returns 404 if the planned course doesn't exist or no template product is found.
    /// </summary>
    [HttpGet("{id}/shop-template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShopTemplate(int id, CancellationToken ct)
    {
        var course = await _plannedCourseQueries.GetByIdAsync(id, ct);
        if (course == null)
            return NotFound(new { error = $"Planned course {id} not found" });

        var sku = await _woo.FindTemplateSkuAsync(course.CourseType, ct);
        if (sku == null)
            return NotFound(new { error = $"No template product found for course type {course.CourseType}" });

        return Ok(new { templateSku = sku });
    }

    /// <summary>
    /// Publish a planned course to Scrum.org.
    /// Requires E-commerce gateway to be published first (needs the WooCommerce product URL).
    /// Creates a course listing via Playwright browser automation.
    /// </summary>
    [HttpPost("{id}/publish/scrumorg")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishScrumOrg(int id)
    {
        try
        {
            var result = await _mediator.Send(new PublishScrumOrgCommand(id));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
