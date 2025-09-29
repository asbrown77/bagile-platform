using Bagile.EtlService;
using Bagile.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<IRawOrderRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection")
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");
    return new RawOrderRepository(connStr!);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var host = builder.Build();

host.MapPost("/webhooks/woo", async (HttpContext http, IRawOrderRepository repo, IConfiguration config, ILogger<Program> logger) =>
{
    // Read body into memory (we need raw bytes for signature verification)
    await using var ms = new MemoryStream();
    await http.Request.Body.CopyToAsync(ms);
    var bodyBytes = ms.ToArray();

    if (bodyBytes.Length == 0)
    {
        logger.LogWarning("Received empty webhook payload");
        http.Response.StatusCode = 400;
        await http.Response.WriteAsync("Empty payload");
        return;
    }

    var maxBytes = config.GetValue<int?>("Webhook:MaxBodySizeBytes") ?? (1 * 1024 * 1024); // 1 MB default
    if (bodyBytes.Length > maxBytes)
    {
        logger.LogWarning("Payload too large: {Size} bytes", bodyBytes.Length);
        http.Response.StatusCode = 413; // Payload Too Large
        await http.Response.WriteAsync("Payload too large");
        return;
    }

    // Optional: verify WooCommerce webhook signature if configured
    var secret = config.GetValue<string>("WooCommerce:WebhookSecret");
    if (!string.IsNullOrEmpty(secret))
    {
        if (!http.Request.Headers.TryGetValue("X-WC-Webhook-Signature", out var sigHeaders) || string.IsNullOrEmpty(sigHeaders))
        {
            logger.LogWarning("Missing X-WC-Webhook-Signature header");
            http.Response.StatusCode = 401;
            await http.Response.WriteAsync("Missing signature");
            return;
        }

        var providedSig = sigHeaders.ToString();
        try
        {
            // WooCommerce sends base64(HMAC-SHA256(payload, secret))
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computed = hmac.ComputeHash(bodyBytes);
            var computedBase64 = Convert.ToBase64String(computed);

            // Constant-time compare
            var prov = Convert.FromBase64String(providedSig);
            var comp = Convert.FromBase64String(computedBase64);
            if (prov.Length != comp.Length || !CryptographicOperations.FixedTimeEquals(prov, comp))
            {
                logger.LogWarning("Invalid webhook signature");
                http.Response.StatusCode = 401;
                await http.Response.WriteAsync("Invalid signature");
                return;
            }
        }
        catch (FormatException)
        {
            logger.LogWarning("Webhook signature header not valid base64");
            http.Response.StatusCode = 401;
            await http.Response.WriteAsync("Invalid signature format");
            return;
        }
    }

    var body = Encoding.UTF8.GetString(bodyBytes);

    string externalId;
    try
    {
        using var doc = JsonDocument.Parse(body);
        externalId = doc.RootElement.GetProperty("id").GetRawText().Trim('"');
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Invalid WooCommerce payload: missing or malformed 'id'");
        http.Response.StatusCode = 400;
        await http.Response.WriteAsync("Invalid WooCommerce payload: missing 'id'");
        return;
    }

    try
    {
        var id = await repo.UpsertAsync("woo", externalId, body);
        logger.LogInformation("Stored raw order from woo externalId={ExternalId} id={Id}", externalId, id);
        await http.Response.WriteAsJsonAsync(new { id });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to upsert raw order externalId={ExternalId}", externalId);
        http.Response.StatusCode = 500;
        await http.Response.WriteAsync("Failed to persist payload");
    }
});

host.MapGet("/debug/raw_orders", async (IRawOrderRepository repo) =>
{
    var all = await repo.GetAllAsync();
    return Results.Json(all.Take(10));
});

host.MapHealthChecks("/health");

host.Run();
