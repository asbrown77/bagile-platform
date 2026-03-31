using Bagile.Application.Analytics.DTOs;
using Bagile.Application.Common.Interfaces;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetOrganisationAnalytics;

public class GetOrganisationAnalyticsQueryHandler
    : IRequestHandler<GetOrganisationAnalyticsQuery, OrganisationAnalyticsResultDto>
{
    private readonly IAnalyticsQueries _queries;

    public GetOrganisationAnalyticsQueryHandler(IAnalyticsQueries queries)
    {
        _queries = queries;
    }

    public async Task<OrganisationAnalyticsResultDto> Handle(
        GetOrganisationAnalyticsQuery request,
        CancellationToken ct)
    {
        int year = request.Year ?? DateTime.UtcNow.Year;
        var orgs = await _queries.GetOrganisationAnalyticsAsync(year, request.SortBy, ct);

        return new OrganisationAnalyticsResultDto
        {
            Year = year,
            Organisations = orgs
        };
    }
}
