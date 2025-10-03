using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bagile.Infrastructure
{
    public interface IRawOrderRepository
    {
        Task<int> InsertAsync(string source, string externalId, string payload, string? eventType = null);
        Task<IEnumerable<RawOrder>> GetAllAsync();
    }
}