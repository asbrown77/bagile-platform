using Bagile.Application.Templates.Commands.SendPreCourseEmail;
using Bagile.Application.Templates.Commands.UpsertPreCourseTemplate;
using Bagile.Application.Templates.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/templates")]
public class PreCourseTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PreCourseTemplatesController(IMediator mediator) => _mediator = mediator;

    /// <summary>List all pre-course email templates.</summary>
    [HttpGet("pre-course")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPreCourseTemplatesQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Get a single pre-course template by course type and format.
    /// Format defaults to 'virtual'. Use ?format=f2f for face-to-face.
    /// </summary>
    [HttpGet("pre-course/{courseType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByType(
        string courseType,
        [FromQuery] string format = "virtual",
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetPreCourseTemplateQuery(courseType, format), ct);

        if (result is null)
            return NotFound(new
            {
                error = $"No pre-course template found for course type '{courseType.ToUpper()}' format '{format}'"
            });

        return Ok(result);
    }

    /// <summary>
    /// Create or update a pre-course template for a given course type and format.
    /// The format ('virtual' or 'f2f') must be included in the request body.
    /// If a template for this (courseType, format) pair already exists it is replaced.
    /// </summary>
    [HttpPut("pre-course/{courseType}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(
        string courseType,
        [FromBody] UpsertPreCourseTemplateRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SubjectTemplate))
            return BadRequest(new { error = "subject_template is required" });

        if (string.IsNullOrWhiteSpace(request.HtmlBody))
            return BadRequest(new { error = "html_body is required" });

        var format = string.IsNullOrWhiteSpace(request.Format) ? "virtual" : request.Format;

        var command = new UpsertPreCourseTemplateCommand
        {
            CourseType      = courseType,
            Format          = format,
            SubjectTemplate = request.SubjectTemplate,
            HtmlBody        = request.HtmlBody
        };

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the full rendered HTML for a pre-course email — with all variables substituted
    /// and wrapped in the b-agile branded template. Used to power the portal's live preview.
    /// Pass htmlBody to preview edited content; omit it to preview the stored template.
    /// </summary>
    [HttpPost("pre-course/preview/{courseScheduleId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Preview(
        long courseScheduleId,
        [FromBody] PreviewPreCourseRequest? request,
        CancellationToken ct)
    {
        try
        {
            var html = await _mediator.Send(new GetPreCourseEmailPreviewQuery
            {
                CourseScheduleId = courseScheduleId,
                HtmlBody         = request?.HtmlBody,
                FormatOverride   = request?.FormatOverride,
            }, ct);

            return Content(html, "text/html");
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
    /// Send the pre-course joining information email to all active attendees on a course.
    /// Loads the template matching the course type and format, pre-fills variables from the
    /// course record (venue, Zoom, dates, trainer), then sends.
    /// CC info@bagile.co.uk automatically.
    /// Pass htmlBodyOverride in the body to send trainer-edited content instead of the stored template.
    /// </summary>
    [HttpPost("pre-course/send/{courseScheduleId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Send(
        long courseScheduleId,
        [FromBody] SendPreCourseRequest? request,
        CancellationToken ct)
    {
        try
        {
            var command = new SendPreCourseEmailCommand
            {
                CourseScheduleId = courseScheduleId,
                FormatOverride   = request?.FormatOverride,
                HtmlBodyOverride = request?.HtmlBodyOverride,
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
    /// Send a test pre-course email to the trainer only (no attendees).
    /// Subject is prefixed with [TEST] so the trainer can verify rendering before the real send.
    /// The recipient is derived from the course's trainer via the trainers table,
    /// or can be overridden in the request body.
    /// </summary>
    [HttpPost("pre-course/test/{courseScheduleId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendTest(
        long courseScheduleId,
        [FromBody] SendPreCourseTestRequest? request,
        CancellationToken ct)
    {
        try
        {
            var command = new SendPreCourseTestEmailCommand
            {
                CourseScheduleId = courseScheduleId,
                FormatOverride   = request?.FormatOverride,
                HtmlBodyOverride = request?.HtmlBodyOverride,
                RecipientEmail   = request?.RecipientEmail,
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

// ── Request models ──────────────────────────────────────────────────────────

public record UpsertPreCourseTemplateRequest
{
    /// <summary>'virtual' or 'f2f'. Defaults to 'virtual' if omitted.</summary>
    public string? Format { get; init; }
    public string SubjectTemplate { get; init; } = "";
    public string HtmlBody { get; init; } = "";
}

public record PreviewPreCourseRequest
{
    public string? HtmlBody { get; init; }
    public string? FormatOverride { get; init; }
}

public record SendPreCourseRequest
{
    /// <summary>Override the format used to select the template ('virtual' or 'f2f').</summary>
    public string? FormatOverride { get; init; }

    /// <summary>
    /// If provided, sent as the email body instead of the stored template.
    /// Supports the compose flow where the trainer edits before sending.
    /// </summary>
    public string? HtmlBodyOverride { get; init; }
}

public record SendPreCourseTestRequest
{
    public string? FormatOverride { get; init; }
    public string? HtmlBodyOverride { get; init; }

    /// <summary>Override the test recipient email. If omitted, derived from the course's trainer.</summary>
    public string? RecipientEmail { get; init; }
}
