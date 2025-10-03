using Bagile.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Bagile.Api;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRawOrderRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection")
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");
    return new RawOrderRepository(connStr!);
});

// Register source-specific webhook handlers
builder.Services.AddSingleton<IWebhookHandler, WooWebhookHandler>();
builder.Services.AddSingleton<IWebhookHandler, XeroWebhookHandler>();
builder.Services.AddSingleton<WebhookHandler>();


builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI();

// Add Kestrel binding for Azure
app.Urls.Clear();
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

// Log what we’re listening on
var logger = app.Logger;
logger.LogInformation("Starting API. Environment URLs: {Urls}", string.Join(", ", app.Urls));
logger.LogInformation("ASPNETCORE_URLS = {AspUrls}", Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
logger.LogInformation("PORT = {Port}", port);

app.MapGet("/", () => Results.Redirect("/swagger"));

// DB test endpoint with host logging
app.MapGet("/dbtest", async (IConfiguration config) =>
{
    var connStr = config.GetConnectionString("DefaultConnection")
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");

    if (string.IsNullOrWhiteSpace(connStr))
        return Results.Problem("❌ No connection string found in configuration.");

    try
    {
        // Parse connection string to extract host
        var builder = new NpgsqlConnectionStringBuilder(connStr);
        var host = builder.Host;

        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();

        return Results.Ok($"✅ Connected successfully to {host}");
    }
    catch (Exception ex)
    {
        var builder = new NpgsqlConnectionStringBuilder(connStr);
        var host = builder.Host;

        return Results.Problem($"❌ Failed. Host: {host} | Error: {ex.Message}");
    }
});

// Generic webhook endpoint
app.MapPost("/webhooks/{source}", async (HttpContext http, string source, WebhookHandler handler) =>
{
    return await handler.HandleAsync(http, source);
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