using Bagile.EtlService;
using Bagile.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// register worker
builder.Services.AddHostedService<Worker>();

// register repo
builder.Services.AddSingleton<IRawOrderRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection")
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");
    return new RawOrderRepository(connStr!);
});

var host = builder.Build();
host.Run();