using Bagile.Application.Common.Interfaces;
using Bagile.Application.Templates;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Templates.Queries;

public record GetFollowUpEmailPreviewQuery : IRequest<string>
{
    public long CourseScheduleId { get; init; }

    /// <summary>
    /// The HTML body to preview. Variables not already substituted will be filled
    /// from the course record. If omitted, the stored template is used.
    /// </summary>
    public string? HtmlBody { get; init; }
}

public class GetFollowUpEmailPreviewQueryHandler
    : IRequestHandler<GetFollowUpEmailPreviewQuery, string>
{
    private readonly ICourseScheduleQueries         _scheduleQueries;
    private readonly IPostCourseTemplateRepository  _templateRepo;

    public GetFollowUpEmailPreviewQueryHandler(
        ICourseScheduleQueries scheduleQueries,
        IPostCourseTemplateRepository templateRepo)
    {
        _scheduleQueries = scheduleQueries;
        _templateRepo    = templateRepo;
    }

    public async Task<string> Handle(GetFollowUpEmailPreviewQuery request, CancellationToken ct)
    {
        var course = await _scheduleQueries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct)
            ?? throw new KeyNotFoundException($"Course schedule {request.CourseScheduleId} not found.");

        var courseType = course.CourseCode.Split('-')[0].ToUpper();

        string bodyTemplate;
        if (!string.IsNullOrWhiteSpace(request.HtmlBody))
        {
            bodyTemplate = request.HtmlBody;
        }
        else
        {
            var template = await _templateRepo.GetByCourseTypeAsync(courseType, ct)
                ?? throw new InvalidOperationException(
                    $"No post-course template found for '{courseType}'.");
            bodyTemplate = template.HtmlBody;
        }

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["greeting"]     = "Hi there",
            ["trainer_name"] = course.TrainerName ?? "Alex and Chris",
            ["course_dates"] = BuildCourseDates(course.StartDate, course.EndDate),
            ["delay_note"]   = "",
            ["course_title"] = course.Title,
            ["course_code"]  = course.CourseCode,
            ["course_type"]  = courseType,
        };

        var html = EmailTemplateWrapper.Wrap(ApplyVariables(bodyTemplate, variables));
        return html;
    }

    private static string BuildCourseDates(DateTime? start, DateTime? end)
    {
        if (start is null) return "";
        if (end is null || end.Value.Date == start.Value.Date)
            return start.Value.ToString("d MMMM yyyy");
        if (start.Value.Month == end.Value.Month && start.Value.Year == end.Value.Year)
            return $"{start.Value.Day}–{end.Value.Day} {start.Value:MMMM yyyy}";
        if (start.Value.Year == end.Value.Year)
            return $"{start.Value:d MMMM} – {end.Value:d MMMM yyyy}";
        return $"{start.Value:d MMMM yyyy} – {end.Value:d MMMM yyyy}";
    }

    private static string ApplyVariables(string template, Dictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
            template = template.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        return template;
    }
}
