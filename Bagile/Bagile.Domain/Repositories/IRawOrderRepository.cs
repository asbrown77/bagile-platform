using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories
{
    public interface IRawOrderRepository
    {
        Task<IEnumerable<RawOrder>> GetUnprocessedAsync(int limit = 100);
        Task MarkProcessedAsync(long id);
        Task MarkFailedAsync(long id, string errorMessage);
        Task<int> InsertAsync(string source, string externalId, string payloadJson, string eventType);
        Task<int> InsertIfChangedAsync(string source, string externalId, string payloadJson, string eventType);
        Task<bool> ExistsAsync(string source, string externalId, string payloadJson);
        Task<DateTime?> GetLastTimestampAsync(string source);
        Task<IEnumerable<RawOrder>> GetAllAsync();
    }


}