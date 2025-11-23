using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface IRawPaymentRepository
{
    Task InsertAsync(RawPayment payment, CancellationToken token);
    Task<IEnumerable<RawPayment>> GetByOrderIdAsync(long orderId, CancellationToken token);
    Task<decimal> GetTotalPaymentForOrderAsync(long orderId, CancellationToken token);
}