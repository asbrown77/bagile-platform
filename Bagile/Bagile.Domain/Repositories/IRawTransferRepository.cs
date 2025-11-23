using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface IRawTransferRepository
{
    Task InsertAsync(RawTransfer transfer, CancellationToken token);
    Task<IEnumerable<RawTransfer>> GetByOrderIdAsync(long orderId, CancellationToken token);
}