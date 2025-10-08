using Bagile.Api.Endpoints;
using Bagile.Api.Handlers;
using Bagile.Api.Services;
using Bagile.Infrastructure.Clients;   
using Bagile.Infrastructure.Repositories;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Repositories
builder.Services.AddSingleton<IRawOrderRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection")
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");
    return new RawOrderRepository(connStr!);
});

// Register external API clients
builder.Services.AddHttpClient<XeroAuthSetupService>();
builder.Services.AddHttpClient<XeroTokenRefreshService>();
builder.Services.AddHttpClient<IXeroApiClient, XeroApiClient>();
builder.Services.AddHttpClient<IWooApiClient, WooApiClient>();

// Handlers
builder.Services.AddSingleton<IWebhookHandler, WooWebhookHandler>();
builder.Services.AddSingleton<IWebhookHandler, XeroWebhookHandler>();
builder.Services.AddSingleton<WebhookHandler>();

// Framework services
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

// Endpoints
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapWebhookEndpoints();
app.MapDiagnosticEndpoints();
app.MapXeroOAuthEndpoints();

app.Run();

public partial class Program { }
