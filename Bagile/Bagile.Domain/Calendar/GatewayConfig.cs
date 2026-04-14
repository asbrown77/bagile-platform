namespace Bagile.Domain.Calendar;

/// <summary>
/// Maps course type prefixes to their applicable publication gateways.
/// Hardcoded for Sprint 26 — settings UI deferred to Sprint 27.
/// </summary>
public static class GatewayConfig
{
    private static readonly HashSet<string> ScrumOrgTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PSM", "PSPO", "PSK", "PALE", "EBM", "APSSD",
        "PSMA", "PSPOA", "PSMAI", "PSPOAI"
    };

    /// <summary>
    /// Returns the list of gateway names applicable to a given course type.
    /// </summary>
    public static IReadOnlyList<string> GetGatewaysForCourseType(string courseType)
    {
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
