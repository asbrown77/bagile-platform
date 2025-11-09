using Microsoft.AspNetCore.Http;
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

    public async Task InvokeAsync(HttpContext context, IConfiguration config)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 1. Allow some paths without authentication
        if (IsExcludedFromAuth(path))
        {
            await _next(context);
            return;
        }

        // 2. Get configured API key from config / env
        var configuredKey = config["ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            // Config issue, not client’s fault
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("API key is not configured.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key is missing.");
            return;
        }

        if (!string.Equals(configuredKey, providedKey.ToString(), StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API key.");
            return;
        }

        // 5. All good, continue down the pipeline
        await _next(context);
    }

    private static bool IsExcludedFromAuth(string path)
    {
        // Paths that should be open
        var skipPaths = new[]
        {
            "/",
            "/health",
            "/swagger",
            "/webhooks/",   // webhooks have their own signing
            "/xero/connect",
            "/xero/callback"
        };

        // StartsWith so "/swagger/index.html" etc are allowed
        return skipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}
