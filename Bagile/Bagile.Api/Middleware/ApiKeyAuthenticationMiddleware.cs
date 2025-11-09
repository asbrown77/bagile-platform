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
        // Skip authentication for specific paths
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (ShouldSkipAuth(path))
        {
            await _next(context);
            return;
        }

        // Check for API key in header
        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key is missing" });
            return;
        }

        var validApiKey = config.GetValue<string>("ApiKey");

        if (string.IsNullOrEmpty(validApiKey))
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "API Key not configured" });
            return;
        }

        if (!validApiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API Key" });
            return;
        }

        await _next(context);
    }

    private static bool ShouldSkipAuth(string path)
    {
        // Allow these paths without authentication
        var skipPaths = new[]
        {
            "/",
            "/health",
            "/swagger",
            "/webhooks/",  // Webhooks use their own signature validation
            "/xero/connect",
            "/xero/callback"
        };

        return skipPaths.Any(p => path.StartsWith(p));
    }
}