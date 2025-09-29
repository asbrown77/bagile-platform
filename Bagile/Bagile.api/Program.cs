using Bagile.Infrastructure;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRawOrderRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection")
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");
    return new RawOrderRepository(connStr!);
});

builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// WooCommerce webhook endpoint
app.MapPost("/webhooks/woo", async (HttpContext http, IConfiguration config, IRawOrderRepository repo, ILogger<Program> logger) =>
{
    await using var ms = new MemoryStream();
    await http.Request.Body.CopyToAsync(ms);
    var bodyBytes = ms.ToArray();

    if (bodyBytes.Length == 0)
    {
        logger.LogWarning("Received empty webhook payload");
        return Results.BadRequest("Empty payload");
    }

    var maxBytes = config.GetValue<int?>("Webhook:MaxBodySizeBytes") ?? (1 * 1024 * 1024);
    if (bodyBytes.Length > maxBytes)
    {
        logger.LogWarning("Payload too large: {Size} bytes", bodyBytes.Length);
        return Results.Problem("Payload too large", statusCode: StatusCodes.Status413PayloadTooLarge);

    }

    var secret = config.GetValue<string>("WooCommerce:WebhookSecret");
    if (!string.IsNullOrEmpty(secret))
    {
        if (!http.Request.Headers.TryGetValue("X-WC-Webhook-Signature", out var sigHeaders) || string.IsNullOrEmpty(sigHeaders))
        {
            logger.LogWarning("Missing webhook signature header");
            return Results.Unauthorized();
        }

        var providedSig = sigHeaders.ToString();
        if (!ValidateSignature(bodyBytes, secret, providedSig))
        {
            logger.LogWarning("Invalid webhook signature");
            return Results.Unauthorized();
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
        return Results.BadRequest("Invalid WooCommerce payload");
    }

    try
    {
        var id = await repo.UpsertAsync("woo", externalId, body);
        logger.LogInformation("Stored raw order externalId={ExternalId}, id={Id}", externalId, id);
        return Results.Ok(new { id });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to upsert raw order externalId={ExternalId}", externalId);
        return Results.Problem("Failed to persist payload", statusCode: StatusCodes.Status500InternalServerError);

    }
});

// Debug endpoint
app.MapGet("/debug/raw_orders", async (IRawOrderRepository repo) =>
{
    var all = await repo.GetAllAsync();
    return Results.Json(all.Take(10));
});

app.MapHealthChecks("/health");

app.Run();

static bool ValidateSignature(byte[] bodyBytes, string secret, string providedSig)
{
    try
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = hmac.ComputeHash(bodyBytes);
        var computedBase64 = Convert.ToBase64String(computed);

        var prov = Convert.FromBase64String(providedSig);
        var comp = Convert.FromBase64String(computedBase64);

        return prov.Length == comp.Length && CryptographicOperations.FixedTimeEquals(prov, comp);
    }
    catch
    {
        return false;
    }
}

public partial class Program { }
