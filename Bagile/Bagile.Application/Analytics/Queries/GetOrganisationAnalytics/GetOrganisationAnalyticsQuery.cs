using Bagile.Application.Analytics.DTOs;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetOrganisationAnalytics;

public record GetOrganisationAnalyticsQuery(
    int? Year = null,
    string SortBy = "spend"
) : IRequest<OrganisationAnalyticsResultDto>;
