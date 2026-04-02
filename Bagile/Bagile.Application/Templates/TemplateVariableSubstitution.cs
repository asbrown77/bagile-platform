namespace Bagile.Application.Templates;

/// <summary>
/// Replaces {{variable}} placeholders in template strings.
/// Case-insensitive key matching. Unknown variables are left as-is.
/// </summary>
public static class TemplateVariableSubstitution
{
    /// <summary>
    /// Replace all {{key}} occurrences in <paramref name="template"/> with values
    /// from <paramref name="variables"/>. Keys are matched case-insensitively.
    /// Placeholders with no matching key are left unchanged.
    /// </summary>
    public static string Apply(string template, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template)) return template;

        foreach (var (key, value) in variables)
            template = template.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);

        return template;
    }
}
