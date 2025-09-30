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

// Register repository with logging of the connection string
builder.Services.AddSingleton<IRawOrderRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    var connStr = config.GetConnectionString("DefaultConnection")
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");

    logger.LogInformation("ETL using connection string: {ConnStr}", connStr);

    return new RawOrderRepository(connStr!);
});

var host = builder.Build();

// Startup log
var loggerStartup = host.Services.GetRequiredService<ILogger<Program>>();
loggerStartup.LogInformation("ETL Service starting up...");

host.Run();