using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Repositories;
using Bagile.Infrastructure.Persistence.Queries;
using Bagile.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bagile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string connectionString)
    {
        // Register query services (read path)
        services.AddScoped<IOrderQueries>(_ => new OrderQueries(connectionString));
        services.AddScoped<ICourseScheduleQueries>(_ => new CourseScheduleQueries(connectionString));
        services.AddScoped<IEnrolmentQueries>(_ => new EnrolmentQueries(connectionString));
        services.AddScoped<IStudentQueries>(_ => new StudentQueries(connectionString));
        services.AddScoped<IOrganisationQueries>(_ => new OrganisationQueries(connectionString));
        services.AddScoped<ITransferQueries>(_ => new TransferQueries(connectionString));

        // Register repositories (write path)
        services.AddScoped<ICourseScheduleRepository>(_ => new CourseScheduleRepository(connectionString));

        return services;
    }
}