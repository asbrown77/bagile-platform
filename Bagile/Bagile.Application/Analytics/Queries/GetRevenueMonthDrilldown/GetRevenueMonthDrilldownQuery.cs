using Bagile.Application.Analytics.DTOs;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetRevenueMonthDrilldown;

public record GetRevenueMonthDrilldownQuery(int Year, int Month) : IRequest<MonthDrilldownDto>;
