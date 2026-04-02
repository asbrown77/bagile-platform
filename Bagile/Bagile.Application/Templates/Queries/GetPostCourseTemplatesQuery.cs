using Bagile.Application.Templates.DTOs;
using MediatR;

namespace Bagile.Application.Templates.Queries;

/// <summary>List all post-course templates.</summary>
public record GetPostCourseTemplatesQuery : IRequest<IEnumerable<PostCourseTemplateDto>>;

/// <summary>Get a single template by course type (e.g. "PSPO").</summary>
public record GetPostCourseTemplateByTypeQuery(string CourseType)
    : IRequest<PostCourseTemplateDto?>;
