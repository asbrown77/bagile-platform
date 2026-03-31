using Bagile.Application.Analytics.DTOs;
using Bagile.Application.Common.Interfaces;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetPartnerAnalytics;

public class GetPartnerAnalyticsQueryHandler
    : IRequestHandler<GetPartnerAnalyticsQuery, IEnumerable<PartnerAnalyticsDto>>
{
    private readonly IAnalyticsQueries _queries;

    public GetPartnerAnalyticsQueryHandler(IAnalyticsQueries queries)
    {
        _queries = queries;
    }

    public async Task<IEnumerable<PartnerAnalyticsDto>> Handle(
        GetPartnerAnalyticsQuery request,
        CancellationToken ct)
    {
        var partners = (await _queries.GetPartnerAnalyticsAsync(ct)).ToList();

        return partners.Select(p => p with
        {
            TierMismatch = !string.IsNullOrEmpty(p.PtnTier)
                && !string.Equals(p.PtnTier, p.CalculatedTier, StringComparison.OrdinalIgnoreCase)
        });
    }
}
