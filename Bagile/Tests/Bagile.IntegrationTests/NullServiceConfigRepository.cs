using Bagile.Domain.Repositories;

namespace Bagile.IntegrationTests;

/// <summary>
/// No-op implementation of IServiceConfigRepository for tests that construct
/// WooApiClient directly and do not need DB-backed credentials.
/// Always returns null / empty — callers fall back to IConfiguration values.
/// </summary>
public sealed class NullServiceConfigRepository : IServiceConfigRepository
{
    public static readonly NullServiceConfigRepository Instance = new();

    private NullServiceConfigRepository() { }

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public Task SetAsync(string key, string value, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>());
}
