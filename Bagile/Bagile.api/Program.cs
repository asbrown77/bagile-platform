using Bagile.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Bagile.Api;

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
    var handler = new WooWebhookHandler(config, repo, logger);
    return await handler.HandleAsync(http);
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
