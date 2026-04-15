using Bagile.Api.Services;
using Microsoft.Extensions.Configuration;

namespace Bagile.Api.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string API_KEY_HEADER = "X-Api-Key";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration config, ApiKeyValidator validator)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (IsExcludedFromAuth(path))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                Code = "AuthenticationFailed",
                Message = "API key is missing. Please provide X-Api-Key header.",
                Status = 401
            });
            return;
        }

        var rawKey = providedKey.ToString();

        // Try database-backed keys first
        var keyInfo = await validator.ValidateAsync(rawKey);
        if (keyInfo != null)
        {
            context.Items["ApiKeyOwner"] = keyInfo.OwnerEmail;
            context.Items["ApiKeyId"] = keyInfo.Id;
            _ = Task.Run(async () => { try { await validator.RecordUsageAsync(keyInfo.Id); } catch { /* swallow — usage tracking must not crash the process */ } });
            await _next(context);
            return;
        }

        // Fallback: legacy config key (remove after all keys migrated to DB)
        var configuredKey = config["ApiKey"];
        if (!string.IsNullOrWhiteSpace(configuredKey) &&
            string.Equals(configuredKey, rawKey, StringComparison.Ordinal))
        {
            context.Items["ApiKeyOwner"] = "legacy-config";
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new
        {
            Code = "AuthenticationFailed",
            Message = "Invalid API key provided.",
            Status = 401
        });
    }

    private static bool IsExcludedFromAuth(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = path.ToLowerInvariant();

        if (path == "/" || path.StartsWith("/health") || path.StartsWith("/swagger"))
            return true;

        return path.StartsWith("/webhooks")
               || path.StartsWith("/api/webhooks")
               || path.StartsWith("/xero")
               || path.StartsWith("/api/xero")
               || path.StartsWith("/portal")
               || path.StartsWith("/api/public");
    }
}
