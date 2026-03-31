using Bagile.Application.Analytics.DTOs;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetPartnerAnalytics;

public record GetPartnerAnalyticsQuery : IRequest<IEnumerable<PartnerAnalyticsDto>>;
