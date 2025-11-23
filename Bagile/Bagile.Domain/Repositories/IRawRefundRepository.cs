using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories
{
    public interface IRawRefundRepository
    {
        Task InsertAsync(long wooOrderId,
            long refundId,
            decimal refundTotal,
            string? refundReason,
            string lineItemsJson,
            string rawJson,
            CancellationToken token);

        Task<IEnumerable<RawRefund>> GetByWooOrderIdAsync(long wooOrderId, CancellationToken token);

        Task<decimal> GetTotalRefundForWooOrderAsync(long wooOrderId, CancellationToken token);
    }
}