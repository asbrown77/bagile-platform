using Bagile.Application.Common.Interfaces;
using Bagile.Application.Templates.Commands.SendPreCourseEmail;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Templates.Queries;

public record GetPreCourseEmailPreviewQuery : IRequest<string>
{
    public long CourseScheduleId { get; init; }

    /// <summary>
    /// The HTML body to preview. Variables not already substituted will be filled
    /// from the course record (agenda, self-study, course_full_name, etc.).
    /// If omitted, the stored template is used.
    /// </summary>
    public string? HtmlBody { get; init; }

    public string? FormatOverride { get; init; }
}

public class GetPreCourseEmailPreviewQueryHandler
    : IRequestHandler<GetPreCourseEmailPreviewQuery, string>
{
    private readonly ICourseScheduleQueries _scheduleQueries;
    private readonly IPreCourseTemplateRepository _templateRepo;

    public GetPreCourseEmailPreviewQueryHandler(
        ICourseScheduleQueries scheduleQueries,
        IPreCourseTemplateRepository templateRepo)
    {
        _scheduleQueries = scheduleQueries;
        _templateRepo    = templateRepo;
    }

    public async Task<string> Handle(GetPreCourseEmailPreviewQuery request, CancellationToken ct)
    {
        var course = await _scheduleQueries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct)
            ?? throw new KeyNotFoundException(
                $"Course schedule {request.CourseScheduleId} not found.");

        var courseType = SendPreCourseEmailCommandHandler.DeriveCourseType(course.CourseCode);
        var format     = SendPreCourseEmailCommandHandler.ResolveFormat(request.FormatOverride, course.FormatType);

        string bodyTemplate;
        if (!string.IsNullOrWhiteSpace(request.HtmlBody))
        {
            bodyTemplate = request.HtmlBody;
        }
        else
        {
            var template = await _templateRepo.GetAsync(courseType, format, ct)
                ?? throw new InvalidOperationException(
                    $"No pre-course template found for '{courseType}' format '{format}'.");
            bodyTemplate = template.HtmlBody;
        }

        var variables = SendPreCourseEmailCommandHandler.BuildVariables(course, courseType);
        var html = EmailTemplateWrapper.Wrap(TemplateVariableSubstitution.Apply(bodyTemplate, variables));
        return html;
    }
}
