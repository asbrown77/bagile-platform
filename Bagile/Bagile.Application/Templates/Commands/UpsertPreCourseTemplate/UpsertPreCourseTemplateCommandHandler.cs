using Bagile.Application.Templates.DTOs;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Templates.Commands.UpsertPreCourseTemplate;

public class UpsertPreCourseTemplateCommandHandler
    : IRequestHandler<UpsertPreCourseTemplateCommand, PreCourseTemplateDto>
{
    private readonly IPreCourseTemplateRepository _repo;

    public UpsertPreCourseTemplateCommandHandler(IPreCourseTemplateRepository repo) => _repo = repo;

    public async Task<PreCourseTemplateDto> Handle(
        UpsertPreCourseTemplateCommand request, CancellationToken ct)
    {
        var entity = new PreCourseTemplate
        {
            CourseType      = request.CourseType.ToUpper(),
            Format          = request.Format.ToLower(),
            SubjectTemplate = request.SubjectTemplate,
            HtmlBody        = request.HtmlBody
        };

        var saved = await _repo.UpsertAsync(entity, ct);

        return new PreCourseTemplateDto
        {
            Id              = saved.Id,
            CourseType      = saved.CourseType,
            Format          = saved.Format,
            SubjectTemplate = saved.SubjectTemplate,
            HtmlBody        = saved.HtmlBody,
            CreatedAt       = saved.CreatedAt,
            UpdatedAt       = saved.UpdatedAt
        };
    }
}
