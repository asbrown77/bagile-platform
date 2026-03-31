using Bagile.Application.Analytics.DTOs;

namespace Bagile.Application.Common.Interfaces;

public interface IRevenueQueries
{
    Task<IEnumerable<MonthlyRevenueDto>> GetMonthlyRevenueAsync(
        int year, CancellationToken ct = default);

    Task<IEnumerable<CourseTypeRevenueDto>> GetRevenueByCourseTypeAsync(
        int year, CancellationToken ct = default);

    Task<IEnumerable<MonthlyRevenueDetailDto>> GetMonthDrilldownAsync(
        int year, int month, CancellationToken ct = default);

    Task<IEnumerable<SourceRevenueDto>> GetRevenueBySourceAsync(
        int year, CancellationToken ct = default);

    Task<IEnumerable<CountryRevenueDto>> GetRevenueByCountryAsync(
        int year, CancellationToken ct = default);
}
