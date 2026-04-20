namespace Bagile.Domain.Calendar;

/// <summary>
/// Maps course provider / course type to their applicable publication gateways.
/// The authoritative method is GetGatewaysForProvider — it reads from the DB-stored
/// provider field on course_definitions. GetGatewaysForCourseType is kept as a
/// compatibility shim for call sites that haven't been migrated yet.
/// </summary>
public static class GatewayConfig
{
    /// <summary>
    /// Returns the list of gateway names applicable to a course given its provider value.
    /// Private courses have no public gateways — they are pre-confirmed B2B bookings.
    /// </summary>
    public static IReadOnlyList<string> GetGatewaysForProvider(string? provider, bool isPrivate)
    {
        if (isPrivate) return Array.Empty<string>();
        return provider switch
        {
            "scrumorg" => new[] { "ecommerce", "scrumorg" },
            "icagile"  => new[] { "ecommerce", "icagile" },
            _          => new[] { "ecommerce" },
        };
    }

    /// <summary>
    /// Compatibility shim — derives provider from course type using legacy hardcoded rules.
    /// Prefer GetGatewaysForProvider when a provider value is available from course_definitions.
    /// </summary>
    public static IReadOnlyList<string> GetGatewaysForCourseType(string courseType, bool isPrivate = false)
    {
        if (isPrivate) return Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(courseType))
            return new[] { "ecommerce" };

        var provider = InferProviderFromCourseType(courseType);
        return GetGatewaysForProvider(provider, isPrivate: false);
    }

    /// <summary>
    /// Infers a provider string from a course type code using legacy naming conventions.
    /// Used only by the shim above — not a substitute for the DB-stored provider.
    /// </summary>
    private static string? InferProviderFromCourseType(string courseType)
    {
        var upper = courseType.ToUpperInvariant().Replace("-", "").Replace("_", "");

        if (upper.StartsWith("ICP")) return "icagile";

        if (ScrumOrgTypesLegacy.Contains(upper)) return "scrumorg";

        return null;
    }

    private static readonly HashSet<string> ScrumOrgTypesLegacy = new(StringComparer.OrdinalIgnoreCase)
    {
        "PSM", "PSPO", "PSK", "PALE", "EBM", "APSSD", "APS",
        "PSMA", "PSPOA", "PSFS", "PALEBM", "PSU", "SPS", "PSPBM",
        // PSMAI and PSPOAI are b-agile AI Essentials — not listed on scrum.org
    };
}
