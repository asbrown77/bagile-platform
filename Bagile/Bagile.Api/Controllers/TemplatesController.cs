using Bagile.Application.Templates.Commands.SendFollowUpEmail;
using Bagile.Application.Templates.Commands.UpsertPostCourseTemplate;
using Bagile.Application.Templates.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/templates")]
public class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TemplatesController(IMediator mediator) => _mediator = mediator;

    /// <summary>List all post-course email templates.</summary>
    [HttpGet("post-course")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPostCourseTemplatesQuery(), ct);
        return Ok(result);
    }

    /// <summary>Get a single post-course template by course type (e.g. PSPO, PSM).</summary>
    [HttpGet("post-course/{courseType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByType(string courseType, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPostCourseTemplateByTypeQuery(courseType), ct);
        if (result is null)
            return NotFound(new { error = $"No template found for course type '{courseType.ToUpper()}'" });

        return Ok(result);
    }

    /// <summary>
    /// Create or update a post-course template for a given course type.
    /// If the course type already exists the template is replaced.
    /// </summary>
    [HttpPut("post-course/{courseType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(
        string courseType,
        [FromBody] UpsertTemplateRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectTemplate))
            return BadRequest(new { error = "subject_template is required" });

        if (string.IsNullOrWhiteSpace(request.HtmlBody))
            return BadRequest(new { error = "html_body is required" });

        var command = new UpsertPostCourseTemplateCommand
        {
            CourseType      = courseType,
            SubjectTemplate = request.SubjectTemplate,
            HtmlBody        = request.HtmlBody
        };

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Send a test follow-up email to the trainer only (no attendees).
    /// Prefixes the subject with [TEST] so the trainer can verify rendering before the real send.
    /// The recipient is derived from the course's trainerName, or can be overridden in the request body.
    /// </summary>
    [HttpPost("post-course/test/{courseScheduleId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendTest(
        long courseScheduleId,
        [FromBody] SendFollowUpTestRequest? request,
        CancellationToken ct)
    {
        try
        {
            var command = new SendFollowUpTestEmailCommand
            {
                CourseScheduleId   = courseScheduleId,
                CourseTypeOverride = request?.CourseTypeOverride,
                HtmlBodyOverride   = request?.HtmlBodyOverride,
                RecipientEmail     = request?.RecipientEmail,
            };

            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Send the follow-up email for a course to all active attendees.
    /// The template is looked up by the course code prefix (e.g. PSPO-300326 → PSPO).
    /// CC info@bagile.co.uk automatically.
    /// </summary>
    [HttpPost("post-course/send/{courseScheduleId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Send(
        long courseScheduleId,
        [FromBody] SendFollowUpRequest? request,
        CancellationToken ct)
    {
        try
        {
            var command = new SendFollowUpEmailCommand
            {
                CourseScheduleId   = courseScheduleId,
                CourseTypeOverride = request?.CourseTypeOverride,
                HtmlBodyOverride   = request?.HtmlBodyOverride,
            };

            var result = await _mediator.Send(command, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

// ── Request models ──────────────────────────────────────────

public record UpsertTemplateRequest
{
    public string SubjectTemplate { get; init; } = "";
    public string HtmlBody { get; init; } = "";
}

public record SendFollowUpRequest
{
    public string? CourseTypeOverride { get; init; }
    public string? HtmlBodyOverride { get; init; }
}

public record SendFollowUpTestRequest
{
    public string? CourseTypeOverride { get; init; }
    public string? HtmlBodyOverride { get; init; }
    public string? RecipientEmail { get; init; }
}
