using Bagile.Application.Dashboard.DTOs;
using MediatR;

namespace Bagile.Application.Dashboard.Queries.GetDashboardOverview;

public record GetDashboardOverviewQuery : IRequest<DashboardOverviewDto>;
