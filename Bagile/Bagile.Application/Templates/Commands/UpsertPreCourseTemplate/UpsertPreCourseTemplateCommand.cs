using Bagile.Application.Templates.DTOs;
using MediatR;

namespace Bagile.Application.Templates.Commands.UpsertPreCourseTemplate;

public record UpsertPreCourseTemplateCommand : IRequest<PreCourseTemplateDto>
{
    public string CourseType { get; init; } = "";
    public string Format { get; init; } = "virtual";
    public string SubjectTemplate { get; init; } = "";
    public string HtmlBody { get; init; } = "";
}
