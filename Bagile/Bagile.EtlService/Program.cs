using Bagile.Domain.Repositories;
using Bagile.EtlService.Collectors;
using Bagile.EtlService.Projectors;
using Bagile.EtlService.Services;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Bagile.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Reflection;

var version = Assembly.GetExecutingAssembly()
    ?.GetName()
    ?.Version?
    .ToString() ?? "unknown";

Console.WriteLine($"Version: {version}");

Console.WriteLine("=== ETL Program started ===");

var builder = Host.CreateApplicationBuilder(args);

ConfigureLogging(builder);
ConfigureDatabase(builder);
ConfigureHttpClients(builder);
ConfigureEtl(builder);

var host = builder.Build();

LogStartup(host);

await host.RunAsync();



static void ConfigureLogging(HostApplicationBuilder builder)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

static void LogStartup(IHost host)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var version = System.Reflection.Assembly.GetExecutingAssembly()
        ?.GetName()
        ?.Version?
        .ToString() ?? "unknown";

    logger.LogInformation("ETL Service starting up... version {Version}", version);
}

static void ConfigureDatabase(HostApplicationBuilder builder)
{
    builder.Services.AddSingleton<IRawOrderRepository>(sp =>
    {
        var connStr = GetConnectionString(sp);
        return new RawOrderRepository(connStr);
    });
    
    builder.Services.AddScoped<IOrderRepository>(sp =>
    {
        var connStr = GetConnectionString(sp);
        return new OrderRepository(connStr);
    });

    builder.Services.AddScoped<IStudentRepository>(sp =>
    {
        var connStr = GetConnectionString(sp);
        return new StudentRepository(connStr);
    });

    builder.Services.AddScoped<IEnrolmentRepository>(sp =>
    {
        var connStr = GetConnectionString(sp);
        return new EnrolmentRepository(connStr);
    });

    builder.Services.AddScoped<ICourseScheduleRepository>(sp =>
    {
        var connStr = GetConnectionString(sp);
        return new CourseScheduleRepository(connStr);
    });

    builder.Services.AddScoped<ICourseDefinitionRepository>(sp =>
    {
        var connStr = GetConnectionString(sp);
        return new CourseDefinitionRepository(connStr);
    });
}

static string GetConnectionString(IServiceProvider sp)
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    var connStr =
        config.GetConnectionString("DefaultConnection")
        ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
        ?? config.GetValue<string>("DbConnectionString")
        ?? config.GetValue<string>("DATABASE_URL")
        ?? throw new InvalidOperationException("Database connection string not configured.");

    LogConnectionInfo(sp, logger, connStr);
    return connStr;
}


static void LogConnectionInfo(IServiceProvider sp, ILogger logger, string connStr)
{
    var env = sp.GetRequiredService<IHostEnvironment>().EnvironmentName;
    var safeConn = new Npgsql.NpgsqlConnectionStringBuilder(connStr);

    if (!string.IsNullOrEmpty(safeConn.Password))
        safeConn.Password = "*****";

    logger.LogInformation("Environment: {Env}. Using connection: {Conn}", env, safeConn);
}


static void ConfigureHttpClients(HostApplicationBuilder builder)
{
    builder.Services.AddHttpClient<IWooApiClient, WooApiClient>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(100);
    });

    builder.Services.AddHttpClient<IXeroApiClient, XeroApiClient>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(100);
    });

    builder.Services.AddHttpClient<XeroTokenRefreshService>();
    builder.Services.AddHttpClient<IFooEventsTicketsClient, FooEventsTicketsClient>();

}

static void ConfigureEtl(HostApplicationBuilder builder)
{
    // Collectors
    builder.Services.AddScoped<IProductCollector, WooProductCollector>();
    builder.Services.AddScoped<ISourceCollector, WooOrderCollector>();
    builder.Services.AddScoped<ISourceCollector, XeroCollector>();

    // Importers
    builder.Services.AddScoped<IImporter<WooProductDto>, WooCourseImporter>();

    // Core orchestration
    builder.Services.AddScoped<RawOrderTransformer>();
    builder.Services.AddScoped<SourceDataImporter>();

    builder.Services.AddHostedService<EtlWorker>();
}
