using Bagile.Application.Common.Interfaces;
using Bagile.Infrastructure.Persistence.Queries;
using Bagile.Domain.Repositories;
using Bagile.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bagile.IntegrationTests;

public static class TestApiFactory
{
    public static WebApplicationFactory<Program> Create(string connectionString,
        Action<IServiceCollection>? configureServices = null,
        Action<IConfigurationBuilder>? configureConfig = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["ConnectionStrings:DefaultConnection"] = connectionString
                    });

                    configureConfig?.Invoke(config);
                });

                builder.ConfigureServices(services =>
                {
                    // Replace DB-backed services
                    var toRemove = services
                        .Where(d =>
                            d.ServiceType == typeof(IRawOrderRepository) ||
                            d.ServiceType == typeof(IOrderQueries))
                        .ToList();

                    foreach (var descriptor in toRemove)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<IRawOrderRepository>(_ => new RawOrderRepository(connectionString));
                    services.AddSingleton<IOrderQueries>(_ => new OrderQueries(connectionString));

                    // Allow per-test-class overrides (e.g. fake Xero client)
                    configureServices?.Invoke(services);
                });
            });
    }
}
