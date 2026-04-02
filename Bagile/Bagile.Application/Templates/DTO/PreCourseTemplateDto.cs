namespace Bagile.Application.Templates.DTOs;

public record PreCourseTemplateDto
{
    public int Id { get; init; }
    public string CourseType { get; init; } = "";
    public string Format { get; init; } = "";
    public string SubjectTemplate { get; init; } = "";
    public string HtmlBody { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
