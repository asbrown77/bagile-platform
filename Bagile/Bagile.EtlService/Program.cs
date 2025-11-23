using Bagile.Domain.Repositories;
using Bagile.EtlService.Collectors;
using Bagile.EtlService.Models;
using Bagile.EtlService.Projectors;
using Bagile.EtlService.Services;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Bagile.Infrastructure.Repositories;
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


// ---------------------- LOGGING ----------------------
static void ConfigureLogging(HostApplicationBuilder builder)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

static void LogStartup(IHost host)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var version = Assembly.GetExecutingAssembly()
        ?.GetName()
        ?.Version?
        .ToString() ?? "unknown";

    logger.LogInformation("ETL Service starting... version {Version}", version);
}


// ---------------------- DATABASE ----------------------
static void ConfigureDatabase(HostApplicationBuilder builder)
{
    builder.Services.AddSingleton<IRawOrderRepository>(sp =>
        new RawOrderRepository(GetConnectionString(sp)));

    builder.Services.AddScoped<IOrderRepository>(sp =>
        new OrderRepository(GetConnectionString(sp)));

    builder.Services.AddScoped<IStudentRepository>(sp =>
        new StudentRepository(GetConnectionString(sp)));

    builder.Services.AddScoped<IEnrolmentRepository>(sp =>
        new EnrolmentRepository(GetConnectionString(sp)));

    builder.Services.AddScoped<ICourseScheduleRepository>(sp =>
        new CourseScheduleRepository(GetConnectionString(sp)));

    builder.Services.AddScoped<ICourseDefinitionRepository>(sp =>
        new CourseDefinitionRepository(GetConnectionString(sp)));

    builder.Services.AddScoped<ISyncMetadataRepository>(sp =>
        new SyncMetadataRepository(GetConnectionString(sp)));

    builder.Services.AddScoped<IRawRefundRepository>(sp =>
        new RawRefundRepository(GetConnectionString(sp)));

    builder.Services.AddScoped<IRawTransferRepository>(sp =>
        new RawTransferRepository(GetConnectionString(sp)));

    builder.Services.AddScoped<IRawPaymentRepository>(sp =>
        new RawPaymentRepository(GetConnectionString(sp)));
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

    var env = sp.GetRequiredService<IHostEnvironment>().EnvironmentName;
    var safe = new Npgsql.NpgsqlConnectionStringBuilder(connStr);

    if (!string.IsNullOrEmpty(safe.Password))
        safe.Password = "*****";

    logger.LogInformation("Environment: {Env}. Using connection: {Conn}", env, safe);
    return connStr;
}


// ---------------------- HTTP CLIENTS ----------------------
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


// ---------------------- NEW ETL PIPELINE ----------------------
static void ConfigureEtl(HostApplicationBuilder builder)
{
    // Collectors
    builder.Services.AddScoped<IProductCollector, WooProductCollector>();
    builder.Services.AddScoped<ISourceCollector, WooOrderCollector>();
    // builder.Services.AddScoped<ISourceCollector, XeroCollector>();

    // Importers
    builder.Services.AddScoped<IImporter<WooProductDto>, WooCourseImporter>();

    // Parsers and processors
    builder.Services.AddScoped<IParser<CanonicalWooOrderDto>, WooOrderParser>();
    builder.Services.AddScoped<IProcessor<CanonicalWooOrderDto>, WooOrderService>();

    builder.Services.AddScoped<IParser<CanonicalXeroInvoiceDto>, XeroInvoiceParser>();
    builder.Services.AddScoped<IProcessor<CanonicalXeroInvoiceDto>, XeroInvoiceService>();

    builder.Services.AddScoped<RawOrderRouter>();

    // Transformer (loops raw orders → parser → service)
    builder.Services.AddScoped<RawOrderTransformer>();
    builder.Services.AddScoped<SourceDataImporter>();

    // Worker (runs ETL cycle)
    builder.Services.AddHostedService<EtlWorker>();
}
