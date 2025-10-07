using Bagile.Infrastructure.Models;

namespace Bagile.Infrastructure.Repositories
{
    public interface IRawOrderRepository
    {
        Task<int> InsertAsync(string source, string externalId, string payloadJson, string eventType);
        Task InsertIfChangedAsync(string source, string externalId, string payloadJson, string eventType);
        Task<bool> ExistsAsync(string source, string externalId, string payloadJson);
        Task<DateTime?> GetLastTimestampAsync(string source);
        Task<IEnumerable<RawOrder>> GetAllAsync();
    }


}