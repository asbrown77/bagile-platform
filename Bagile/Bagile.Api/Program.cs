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

// DB test endpoint
app.MapGet("/dbtest", async (IConfiguration config) =>
{
    var connStr = config.GetConnectionString("DefaultConnection")
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");

    try
    {
        using var conn = new NpgsqlConnection(connStr);
        await conn.OpenAsync();
        return Results.Ok("✅ API can connect to Postgres!");
    }
    catch (Exception ex)
    {
        return Results.Problem($"❌ Failed: {ex.Message}");
    }
});

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