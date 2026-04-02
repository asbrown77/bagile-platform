using Bagile.Application.Templates.DTOs;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Templates.Queries;

public class GetPostCourseTemplatesQueryHandler
    : IRequestHandler<GetPostCourseTemplatesQuery, IEnumerable<PostCourseTemplateDto>>
{
    private readonly IPostCourseTemplateRepository _repo;

    public GetPostCourseTemplatesQueryHandler(IPostCourseTemplateRepository repo) => _repo = repo;

    public async Task<IEnumerable<PostCourseTemplateDto>> Handle(
        GetPostCourseTemplatesQuery request, CancellationToken ct)
    {
        var templates = await _repo.GetAllAsync(ct);
        return templates.Select(Map);
    }

    private static PostCourseTemplateDto Map(Domain.Entities.PostCourseTemplate t) => new()
    {
        Id              = t.Id,
        CourseType      = t.CourseType,
        SubjectTemplate = t.SubjectTemplate,
        HtmlBody        = t.HtmlBody,
        CreatedAt       = t.CreatedAt,
        UpdatedAt       = t.UpdatedAt
    };
}

public class GetPostCourseTemplateByTypeQueryHandler
    : IRequestHandler<GetPostCourseTemplateByTypeQuery, PostCourseTemplateDto?>
{
    private readonly IPostCourseTemplateRepository _repo;

    public GetPostCourseTemplateByTypeQueryHandler(IPostCourseTemplateRepository repo) => _repo = repo;

    public async Task<PostCourseTemplateDto?> Handle(
        GetPostCourseTemplateByTypeQuery request, CancellationToken ct)
    {
        var t = await _repo.GetByCourseTypeAsync(request.CourseType.ToUpper(), ct);
        if (t is null) return null;

        return new PostCourseTemplateDto
        {
            Id              = t.Id,
            CourseType      = t.CourseType,
            SubjectTemplate = t.SubjectTemplate,
            HtmlBody        = t.HtmlBody,
            CreatedAt       = t.CreatedAt,
            UpdatedAt       = t.UpdatedAt
        };
    }
}
