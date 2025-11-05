using Bagile.Application.Common.Interfaces;
using Bagile.Infrastructure.Persistence.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bagile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string connectionString)
    {
        // Register query services
        services.AddScoped<IOrderQueries>(_ => new OrderQueries(connectionString));

        return services;
    }
}