using Bagile.Application.Templates.DTOs;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Templates.Queries;

public class GetPreCourseTemplatesQueryHandler
    : IRequestHandler<GetPreCourseTemplatesQuery, IEnumerable<PreCourseTemplateDto>>
{
    private readonly IPreCourseTemplateRepository _repo;

    public GetPreCourseTemplatesQueryHandler(IPreCourseTemplateRepository repo) => _repo = repo;

    public async Task<IEnumerable<PreCourseTemplateDto>> Handle(
        GetPreCourseTemplatesQuery request, CancellationToken ct)
    {
        var templates = await _repo.GetAllAsync(ct);
        return templates.Select(Map);
    }

    internal static PreCourseTemplateDto Map(Domain.Entities.PreCourseTemplate t) => new()
    {
        Id              = t.Id,
        CourseType      = t.CourseType,
        Format          = t.Format,
        SubjectTemplate = t.SubjectTemplate,
        HtmlBody        = t.HtmlBody,
        CreatedAt       = t.CreatedAt,
        UpdatedAt       = t.UpdatedAt
    };
}

public class GetPreCourseTemplateQueryHandler
    : IRequestHandler<GetPreCourseTemplateQuery, PreCourseTemplateDto?>
{
    private readonly IPreCourseTemplateRepository _repo;

    public GetPreCourseTemplateQueryHandler(IPreCourseTemplateRepository repo) => _repo = repo;

    public async Task<PreCourseTemplateDto?> Handle(
        GetPreCourseTemplateQuery request, CancellationToken ct)
    {
        var t = await _repo.GetAsync(
            request.CourseType.ToUpper(),
            request.Format.ToLower(),
            ct);

        return t is null ? null : GetPreCourseTemplatesQueryHandler.Map(t);
    }
}
