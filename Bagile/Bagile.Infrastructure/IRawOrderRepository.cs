using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bagile.Infrastructure
{
    public interface IRawOrderRepository
    {
        Task<int> UpsertAsync(string source, string externalId, string payload);
        Task<IEnumerable<RawOrder>> GetAllAsync();
    }
}