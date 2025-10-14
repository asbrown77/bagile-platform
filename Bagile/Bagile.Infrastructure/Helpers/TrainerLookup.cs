namespace Bagile.Infrastructure.Helpers;

public static class TrainerLookup
{
    private static readonly Dictionary<string, string> TrainerMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "AB", "Alex Brown" },
        { "CB", "Chris Bexon" },
    };

    public static string? FromSku(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        var parts = sku.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;

        var code = parts[^1].Trim();
        return TrainerMap.TryGetValue(code, out var name) ? name : null;
    }
}