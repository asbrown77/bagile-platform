namespace Bagile.Domain.Entities;

public class PreCourseTemplate
{
    public int Id { get; set; }

    /// <summary>
    /// Short course type key: PSM, PSPO, PSK, etc.
    /// </summary>
    public string CourseType { get; set; } = "";

    /// <summary>
    /// Delivery format: 'virtual' or 'f2f'.
    /// Together with CourseType forms the unique key.
    /// </summary>
    public string Format { get; set; } = "virtual";

    /// <summary>
    /// Subject line — may contain {{course_name}}, {{dates}}, {{client_name}} etc.
    /// </summary>
    public string SubjectTemplate { get; set; } = "";

    /// <summary>
    /// Full HTML body with {{variable}} placeholders.
    /// Variables: {{course_name}}, {{dates}}, {{times}}, {{trainer_name}},
    /// {{venue_address}} (f2f), {{zoom_url}}/{{zoom_id}}/{{zoom_passcode}} (virtual),
    /// {{client_name}}, {{self_study}}, {{agenda}}.
    /// </summary>
    public string HtmlBody { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
