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
                    // Replace DB-backed services with test versions
                    var toRemove = services
                        .Where(d =>
                            d.ServiceType == typeof(IRawOrderRepository) ||
                            d.ServiceType == typeof(IOrderQueries) ||
                            d.ServiceType == typeof(ICourseScheduleQueries) ||
                            d.ServiceType == typeof(IEnrolmentQueries) ||
                            d.ServiceType == typeof(IStudentQueries) ||
                            d.ServiceType == typeof(IOrganisationQueries) ||
                            d.ServiceType == typeof(ITransferQueries))
                        .ToList();

                    foreach (var descriptor in toRemove)
                    {
                        services.Remove(descriptor);
                    }

                    // Register all query services
                    services.AddSingleton<IRawOrderRepository>(_ => new RawOrderRepository(connectionString));
                    services.AddSingleton<IOrderQueries>(_ => new OrderQueries(connectionString));
                    services.AddSingleton<ICourseScheduleQueries>(_ => new CourseScheduleQueries(connectionString));
                    services.AddSingleton<IEnrolmentQueries>(_ => new EnrolmentQueries(connectionString));
                    services.AddSingleton<IStudentQueries>(_ => new StudentQueries(connectionString));
                    services.AddSingleton<IOrganisationQueries>(_ => new OrganisationQueries(connectionString));
                    services.AddSingleton<ITransferQueries>(_ => new TransferQueries(connectionString));

                    // Allow per-test-class overrides (e.g. fake Xero client)
                    configureServices?.Invoke(services);
                });
            });
    }
}