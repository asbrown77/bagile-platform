using System;
using Bagile.EtlService.Collectors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Bagile.Infrastructure;
using Bagile.Infrastructure.Repositories;
using Bagile.Infrastructure.Clients;
using Bagile.EtlService.Services;
using ISourceCollector = Bagile.EtlService.Collectors.ISourceCollector; // where EtlWorker and EtlRunner live

Console.WriteLine("=== ETL Program started ===");

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Repository
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

// Http clients
builder.Services.AddHttpClient<IWooApiClient, WooApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(100);
});

builder.Services.AddHttpClient<IXeroApiClient, XeroApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(100);
});

// Collectors
builder.Services.AddScoped<ISourceCollector, WooCollector>();
builder.Services.AddScoped<ISourceCollector, XeroCollector>();

// Core ETL orchestrator
builder.Services.AddScoped<EtlRunner>();

// Background service
builder.Services.AddHostedService<EtlWorker>();

var host = builder.Build();

var loggerStartup = host.Services.GetRequiredService<ILogger<Program>>();
loggerStartup.LogInformation("ETL Service starting up...");

host.Run();