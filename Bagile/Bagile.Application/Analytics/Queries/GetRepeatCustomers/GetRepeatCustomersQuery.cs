using Bagile.Application.Analytics.DTOs;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetRepeatCustomers;

public record GetRepeatCustomersQuery(
    int? Year = null,
    int MinBookings = 2
) : IRequest<IEnumerable<RepeatCustomerDto>>;
