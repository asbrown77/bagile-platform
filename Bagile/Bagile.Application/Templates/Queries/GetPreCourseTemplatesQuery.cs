using Bagile.Application.Templates.DTOs;
using MediatR;

namespace Bagile.Application.Templates.Queries;

/// <summary>List all pre-course templates.</summary>
public record GetPreCourseTemplatesQuery : IRequest<IEnumerable<PreCourseTemplateDto>>;

/// <summary>Get a single pre-course template by course type and format.</summary>
public record GetPreCourseTemplateQuery(string CourseType, string Format)
    : IRequest<PreCourseTemplateDto?>;
