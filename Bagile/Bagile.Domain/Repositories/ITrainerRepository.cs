using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface ITrainerRepository
{
    Task<IEnumerable<Trainer>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Trainer?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Trainer> CreateAsync(Trainer trainer, CancellationToken ct = default);
    Task<Trainer?> UpdateAsync(Trainer trainer, CancellationToken ct = default);

    /// <summary>
    /// Soft-delete: sets is_active = false. Returns false if the trainer was not found.
    /// </summary>
    Task<bool> DeactivateAsync(int id, CancellationToken ct = default);
}
