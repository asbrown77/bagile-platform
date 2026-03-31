using Bagile.Application.Analytics.DTOs;
using Bagile.Application.Common.Interfaces;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetRepeatCustomers;

public class GetRepeatCustomersQueryHandler
    : IRequestHandler<GetRepeatCustomersQuery, IEnumerable<RepeatCustomerDto>>
{
    private readonly IAnalyticsQueries _queries;

    public GetRepeatCustomersQueryHandler(IAnalyticsQueries queries)
    {
        _queries = queries;
    }

    public async Task<IEnumerable<RepeatCustomerDto>> Handle(
        GetRepeatCustomersQuery request,
        CancellationToken ct)
    {
        int year = request.Year ?? DateTime.UtcNow.Year;
        return await _queries.GetRepeatCustomersAsync(year, request.MinBookings, ct);
    }
}
