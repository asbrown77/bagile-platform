using System.Text.Json;

namespace Bagile.EtlService.Utils;

public static class JsonHelpers
{
    public static string ExtractId(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("id", out var id)
            ? id.GetRawText()
            : Guid.NewGuid().ToString();
    }
}

