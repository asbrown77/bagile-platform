using Bagile.Domain.Repositories;

namespace Bagile.Api.Endpoints;

public static class SettingsEndpoints
{
    private const string MaskValue = "********";

    public static void MapSettingsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/settings/service-config", GetAll);
        app.MapPut("/api/settings/service-config/{key}", Set);
    }

    private static async Task<IResult> GetAll(IServiceConfigRepository repo)
    {
        var all = await repo.GetAllAsync();

        var result = all.ToDictionary(
            kv => kv.Key,
            kv => IsSensitiveKey(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value)
                ? MaskValue
                : kv.Value);

        return Results.Ok(result);
    }

    private static async Task<IResult> Set(
        string key,
        HttpContext context,
        IServiceConfigRepository repo)
    {
        var body = await context.Request.ReadFromJsonAsync<SetValueRequest>();
        if (body is null)
            return Results.BadRequest(new { error = "Request body is required" });

        // Never persist the mask sentinel — it means "leave unchanged".
        if (body.Value == MaskValue)
            return Results.NoContent();

        await repo.SetAsync(key, body.Value ?? "");
        return Results.NoContent();
    }

    private static bool IsSensitiveKey(string key)
    {
        var lower = key.ToLowerInvariant();
        return lower.Contains("password") || lower.Contains("secret");
    }

    private record SetValueRequest(string? Value);
}
