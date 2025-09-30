using Bagile.EtlService;
using Bagile.Infrastructure;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== ETL Program started ===");

var builder = Host.CreateApplicationBuilder(args);

// Configure logging (so Azure shows your messages)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Register worker
builder.Services.AddHostedService<Worker>();

// Register repository
builder.Services.AddSingleton<IRawOrderRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection")
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");
    return new RawOrderRepository(connStr!);
});

var host = builder.Build();

// Startup log
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("ETL Service starting up...");

host.Run();