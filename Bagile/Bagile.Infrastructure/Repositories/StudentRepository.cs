using Dapper;
using Npgsql;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;

namespace Bagile.Infrastructure.Repositories
{
    public class StudentRepository : IStudentRepository
    {
        private readonly string _conn;
        public StudentRepository(string conn) => _conn = conn;

        /// <summary>
        /// ETL upsert — skips fields marked as overridden so manual corrections survive re-sync.
        /// </summary>
        public async Task<long> UpsertAsync(Student student)
        {
            // Upsert by email. For each overrideable field we only update if it is NOT
            // already flagged in overridden_fields. This keeps manual corrections intact.
            const string sql = @"
            INSERT INTO bagile.students (email, first_name, last_name, company, country)
            VALUES (@Email, @FirstName, @LastName, @Company, @Country)
            ON CONFLICT (email) DO UPDATE
            SET first_name = CASE
                    WHEN bagile.students.overridden_fields ? 'first_name' THEN bagile.students.first_name
                    ELSE EXCLUDED.first_name
                END,
                last_name = CASE
                    WHEN bagile.students.overridden_fields ? 'last_name' THEN bagile.students.last_name
                    ELSE EXCLUDED.last_name
                END,
                company = CASE
                    WHEN bagile.students.overridden_fields ? 'company' THEN bagile.students.company
                    ELSE EXCLUDED.company
                END,
                country = CASE
                    WHEN bagile.students.overridden_fields ? 'country' THEN bagile.students.country
                    ELSE COALESCE(EXCLUDED.country, bagile.students.country)
                END
            RETURNING id;";

            await using var c = new NpgsqlConnection(_conn);
            return await c.ExecuteScalarAsync<long>(sql, student);
        }

        public async Task<Student?> GetByIdAsync(long id)
        {
            const string sql = @"
            SELECT id, email, first_name AS FirstName, last_name AS LastName,
                   company, country,
                   overridden_fields AS OverriddenFields,
                   updated_by AS UpdatedBy,
                   override_note AS OverrideNote
            FROM bagile.students
            WHERE id = @Id;";

            await using var c = new NpgsqlConnection(_conn);
            return await c.QuerySingleOrDefaultAsync<Student>(sql, new { Id = id });
        }

        /// <summary>
        /// Portal-initiated override. Updates only the provided (non-null) fields and
        /// records which fields are now overridden so ETL skips them on the next sync.
        /// </summary>
        public async Task<Student?> OverrideAsync(long id, StudentOverride @override, CancellationToken ct = default)
        {
            // Build the override_fields merge patch so we only flag what was actually changed.
            // We use jsonb_set to add flags without wiping existing ones.
            var updates = new List<string>();
            var parameters = new DynamicParameters();
            parameters.Add("id", id);

            if (@override.Email is not null)
            {
                updates.Add("email = @email");
                updates.Add("overridden_fields = overridden_fields || '{\"email\": true}'::jsonb");
                parameters.Add("email", @override.Email);
            }
            if (@override.FirstName is not null)
            {
                updates.Add("first_name = @firstName");
                updates.Add("overridden_fields = overridden_fields || '{\"first_name\": true}'::jsonb");
                parameters.Add("firstName", @override.FirstName);
            }
            if (@override.LastName is not null)
            {
                updates.Add("last_name = @lastName");
                updates.Add("overridden_fields = overridden_fields || '{\"last_name\": true}'::jsonb");
                parameters.Add("lastName", @override.LastName);
            }
            if (@override.Company is not null)
            {
                updates.Add("company = @company");
                updates.Add("overridden_fields = overridden_fields || '{\"company\": true}'::jsonb");
                parameters.Add("company", @override.Company);
            }
            if (@override.UpdatedBy is not null)
            {
                updates.Add("updated_by = @updatedBy");
                parameters.Add("updatedBy", @override.UpdatedBy);
            }
            if (@override.OverrideNote is not null)
            {
                updates.Add("override_note = @overrideNote");
                parameters.Add("overrideNote", @override.OverrideNote);
            }

            if (updates.Count == 0) return await GetByIdAsync(id);

            // Deduplicate — the jsonb merge expressions above get repeated if multiple fields
            // are set. Postgres handles duplicate SET clauses by taking the last one,
            // but we produce distinct jsonb merges so deduplicate by keeping uniques.
            var distinctUpdates = updates.Distinct().ToList();

            var sql = $@"
            UPDATE bagile.students
            SET {string.Join(", ", distinctUpdates)},
                updated_at = NOW()
            WHERE id = @id
            RETURNING id, email, first_name AS FirstName, last_name AS LastName,
                      company, country,
                      overridden_fields AS OverriddenFields,
                      updated_by AS UpdatedBy,
                      override_note AS OverrideNote;";

            await using var c = new NpgsqlConnection(_conn);
            return await c.QuerySingleOrDefaultAsync<Student>(
                new CommandDefinition(sql, parameters, cancellationToken: ct));
        }
    }
}
