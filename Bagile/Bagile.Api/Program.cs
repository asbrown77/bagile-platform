using Bagile.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Text.Json;
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

public partial class Program { }
