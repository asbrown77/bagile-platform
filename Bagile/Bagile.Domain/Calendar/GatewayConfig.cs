namespace Bagile.Domain.Calendar;

/// <summary>
/// Maps course type prefixes to their applicable publication gateways.
/// Hardcoded for Sprint 26 — settings UI deferred to Sprint 27.
/// </summary>
public static class GatewayConfig
{
    private static readonly HashSet<string> ScrumOrgTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PSM", "PSPO", "PSK", "PALE", "EBM", "APSSD", "APS",
        "PSMA", "PSPOA", "PSFS"
        // PSMAI and PSPOAI are b-agile AI Essentials courses — not listed on scrum.org
    };

    /// <summary>
    /// Returns the list of gateway names applicable to a given course type.
    /// Private courses have no public gateways — they are pre-confirmed B2B bookings.
    /// </summary>
    public static IReadOnlyList<string> GetGatewaysForCourseType(string courseType, bool isPrivate = false)
    {
        if (isPrivate)
            return Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(courseType))
            return new[] { "ecommerce" };

        var upper = courseType.ToUpperInvariant();

        if (ScrumOrgTypes.Contains(upper))
            return new[] { "ecommerce", "scrumorg" };

        if (upper.StartsWith("ICP"))
            return new[] { "ecommerce", "icagile" };

        return new[] { "ecommerce" };
    }
}
