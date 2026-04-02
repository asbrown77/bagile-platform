using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class TrainerRepository : ITrainerRepository
{
    private readonly string _conn;

    public TrainerRepository(string conn) => _conn = conn;

    public async Task<IEnumerable<Trainer>> GetAllActiveAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name, email, phone, is_active AS IsActive, created_at AS CreatedAt
            FROM bagile.trainers
            WHERE is_active = TRUE
            ORDER BY name;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QueryAsync<Trainer>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<Trainer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name, email, phone, is_active AS IsActive, created_at AS CreatedAt
            FROM bagile.trainers
            WHERE id = @id;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleOrDefaultAsync<Trainer>(
            new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task<Trainer> CreateAsync(Trainer trainer, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO bagile.trainers (name, email, phone, is_active)
            VALUES (@Name, @Email, @Phone, TRUE)
            RETURNING id, name, email, phone, is_active AS IsActive, created_at AS CreatedAt;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleAsync<Trainer>(
            new CommandDefinition(sql, trainer, cancellationToken: ct));
    }

    public async Task<Trainer?> UpdateAsync(Trainer trainer, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE bagile.trainers
            SET name  = @Name,
                email = @Email,
                phone = @Phone
            WHERE id = @Id
            RETURNING id, name, email, phone, is_active AS IsActive, created_at AS CreatedAt;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleOrDefaultAsync<Trainer>(
            new CommandDefinition(sql, trainer, cancellationToken: ct));
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE bagile.trainers SET is_active = FALSE WHERE id = @id;";

        await using var c = new NpgsqlConnection(_conn);
        var rows = await c.ExecuteAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct));
        return rows > 0;
    }
}
