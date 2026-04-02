using Bagile.Application.Templates.DTOs;
using MediatR;

namespace Bagile.Application.Templates.Commands.UpsertPostCourseTemplate;

public record UpsertPostCourseTemplateCommand : IRequest<PostCourseTemplateDto>
{
    public string CourseType { get; init; } = "";
    public string SubjectTemplate { get; init; } = "";
    public string HtmlBody { get; init; } = "";
}
