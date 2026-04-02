namespace Bagile.Domain.Entities;

public class PostCourseTemplate
{
    public int Id { get; set; }

    /// <summary>
    /// Short course type key: PSM, PSPO, PSPOA, PSU, PALEBM, PSK, PALE, PSMA, PSMAI, PSPOAI, PSFS, APSD
    /// </summary>
    public string CourseType { get; set; } = "";

    /// <summary>
    /// Subject line — may contain {{trainer_name}}, {{course_dates}} etc.
    /// </summary>
    public string SubjectTemplate { get; set; } = "";

    /// <summary>
    /// Full HTML body with {{variable}} placeholders.
    /// </summary>
    public string HtmlBody { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
