using Bagile.Application.Analytics.DTOs;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetRevenueSummary;

public record GetRevenueSummaryQuery(int? Year = null) : IRequest<RevenueSummaryDto>;
