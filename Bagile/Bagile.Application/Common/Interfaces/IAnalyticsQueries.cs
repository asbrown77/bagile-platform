using Bagile.Application.Analytics.DTOs;

namespace Bagile.Application.Common.Interfaces;

public interface IAnalyticsQueries
{
    Task<IEnumerable<OrganisationAnalyticsDto>> GetOrganisationAnalyticsAsync(
        int year, string sortBy = "spend", CancellationToken ct = default);

    Task<IEnumerable<PartnerAnalyticsDto>> GetPartnerAnalyticsAsync(
        CancellationToken ct = default);

    Task<IEnumerable<CourseDemandDto>> GetCourseDemandAsync(
        int lookbackMonths = 12, CancellationToken ct = default);

    Task<IEnumerable<CourseDemandMonthlyDto>> GetCourseDemandMonthlyAsync(
        int lookbackMonths = 12, CancellationToken ct = default);

    Task<IEnumerable<RepeatCustomerDto>> GetRepeatCustomersAsync(
        int year, int minBookings = 2, CancellationToken ct = default);
}
