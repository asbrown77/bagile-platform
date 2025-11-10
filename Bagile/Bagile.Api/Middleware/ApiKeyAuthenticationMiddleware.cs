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
            // Config issue, not client's fault
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                Code = "ConfigurationError",
                Message = "API key is not configured on server",
                Status = 500
            });
            return;
        }

        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                Code = "AuthenticationFailed",
                Message = "API key is missing. Please provide X-Api-Key header.",
                Status = 401
            });
            return;
        }

        if (!string.Equals(configuredKey, providedKey.ToString(), StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                Code = "AuthenticationFailed",
                Message = "Invalid API key provided.",
                Status = 401
            });
            return;
        }

        // 5. All good, continue down the pipeline
        await _next(context);
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
               || path.StartsWith("/api/xero");
    }
}