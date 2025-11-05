using Bagile.Api.Handlers;
using Bagile.Api.Services;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Repositories;
using Npgsql;
using System.Reflection;
using Bagile.Domain.Repositories;
using Bagile.Application;
using Bagile.Infrastructure;
using Bagile.Api.Endpoints;

var version = Assembly.GetExecutingAssembly()
    ?.GetName()
    ?.Version?
    .ToString() ?? "unknown";

Console.WriteLine($"Version: {version}");
Console.WriteLine("=== API Program started ===");

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")
                       ?? builder.Configuration.GetValue<string>("DbConnectionString")
                       ?? throw new InvalidOperationException("Database connection string not found.");


// Repositories
builder.Services.AddSingleton<IRawOrderRepository>(_ => new RawOrderRepository(connectionString));

// Register external API clients
builder.Services.AddHttpClient<XeroAuthSetupService>();
builder.Services.AddHttpClient<XeroTokenRefreshService>();
builder.Services.AddHttpClient<IXeroApiClient, XeroApiClient>();
builder.Services.AddHttpClient<IWooApiClient, WooApiClient>();

// Handlers
builder.Services.AddSingleton<IWebhookHandler, WooWebhookHandler>();
builder.Services.AddSingleton<IWebhookHandler, XeroWebhookHandler>();
builder.Services.AddSingleton<WebhookHandler>();

builder.Services.AddApplicationServices();        // Registers MediatR
builder.Services.AddInfrastructureServices(connectionString);  // Registers IOrderQueries

// Framework services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Log startup with version info
app.Logger.LogInformation("API Service starting up... version {Version}", version);

// Detailed errors for dev & test
if (app.Environment.IsDevelopment() ||
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
}

// Optional: simple production-safe error endpoint
app.Map("/error", () =>
    Results.Problem("An unexpected error occurred. Please contact support."));

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Console logging for test visibility
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"🔥 Exception: {ex}");
        throw;
    }
});

// Azure port binding
if (app.Environment.IsProduction())
{
    app.Urls.Clear();
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    app.Urls.Add($"http://0.0.0.0:{port}");
    app.Logger.LogInformation("Starting API on port {Port}", port);
}
else
{
    app.Logger.LogInformation("Running locally on default Kestrel ports (5000/5001).");
}

app.MapControllers();  // enables MVC controllers

// Keep minimal APIs if you like (e.g. diagnostics, webhooks)
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapWebhookEndpoints();
app.MapDiagnosticEndpoints();
app.MapXeroOAuthEndpoints();

app.Run();

public partial class Program { }
