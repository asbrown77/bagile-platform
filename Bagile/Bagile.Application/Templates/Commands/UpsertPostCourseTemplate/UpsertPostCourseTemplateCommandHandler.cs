using Bagile.Application.Templates.DTOs;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Templates.Commands.UpsertPostCourseTemplate;

public class UpsertPostCourseTemplateCommandHandler
    : IRequestHandler<UpsertPostCourseTemplateCommand, PostCourseTemplateDto>
{
    private readonly IPostCourseTemplateRepository _repo;

    public UpsertPostCourseTemplateCommandHandler(IPostCourseTemplateRepository repo) => _repo = repo;

    public async Task<PostCourseTemplateDto> Handle(
        UpsertPostCourseTemplateCommand request, CancellationToken ct)
    {
        var entity = new PostCourseTemplate
        {
            CourseType      = request.CourseType.ToUpper(),
            SubjectTemplate = request.SubjectTemplate,
            HtmlBody        = request.HtmlBody
        };

        var saved = await _repo.UpsertAsync(entity, ct);

        return new PostCourseTemplateDto
        {
            Id              = saved.Id,
            CourseType      = saved.CourseType,
            SubjectTemplate = saved.SubjectTemplate,
            HtmlBody        = saved.HtmlBody,
            CreatedAt       = saved.CreatedAt,
            UpdatedAt       = saved.UpdatedAt
        };
    }
}
