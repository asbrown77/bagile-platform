using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Repositories;
using Bagile.Infrastructure.Email;
using Bagile.Infrastructure.Persistence.Queries;
using Bagile.Infrastructure.Repositories;
using Bagile.Infrastructure.Services;
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
        services.AddScoped<IRevenueQueries>(_ => new RevenueQueries(connectionString));
        services.AddScoped<IAnalyticsQueries>(_ => new AnalyticsQueries(connectionString));
        services.AddScoped<ICalendarQueries>(sp =>
        {
            var cfg = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var wooSiteUrl = cfg["WooCommerce:BaseUrl"] ?? "";
            return new CalendarQueries(connectionString, wooSiteUrl);
        });
        services.AddScoped<IPlannedCourseQueries>(_ => new PlannedCourseQueries(connectionString));

        // Register repositories (write path)
        services.AddScoped<ICourseScheduleRepository>(_ => new CourseScheduleRepository(connectionString));
        services.AddScoped<IStudentRepository>(_ => new StudentRepository(connectionString));
        services.AddScoped<IEnrolmentRepository>(_ => new EnrolmentRepository(connectionString));
        services.AddScoped<IPostCourseTemplateRepository>(_ => new PostCourseTemplateRepository(connectionString));
        services.AddScoped<ICourseContactRepository>(_ => new CourseContactRepository(connectionString));
        services.AddScoped<ITrainerRepository>(_ => new TrainerRepository(connectionString));
        services.AddScoped<IPreCourseTemplateRepository>(_ => new PreCourseTemplateRepository(connectionString));
        services.AddScoped<IEmailSendLogRepository>(_ => new EmailSendLogRepository(connectionString));
        services.AddScoped<IOrganisationRepository>(_ => new OrganisationRepository(connectionString));
        services.AddScoped<IPlannedCourseRepository>(_ => new PlannedCourseRepository(connectionString));
        services.AddScoped<ICoursePublicationRepository>(_ => new CoursePublicationRepository(connectionString));
        services.AddSingleton<IServiceConfigRepository>(_ => new ServiceConfigRepository(connectionString));

        // Publish services (WooCommerce + Scrum.org)
        services.AddScoped<IWooCommercePublishService, WooCommercePublishService>();
        services.AddScoped<IScrumOrgPublishService, ScrumOrgPublishService>();

        // Email service (SMTP — see Smtp:* in appsettings)
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}