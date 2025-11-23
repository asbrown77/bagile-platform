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

        public async Task<long> UpsertAsync(Student student)
        {
            const string sql = @"
            INSERT INTO bagile.students (email, first_name, last_name, company)
            VALUES (@Email, @FirstName, @LastName, @Company)
            ON CONFLICT (email) DO UPDATE
            SET first_name = EXCLUDED.first_name,
                last_name = EXCLUDED.last_name,
                company = EXCLUDED.company
            RETURNING id;";

            await using var c = new NpgsqlConnection(_conn);
            return await c.ExecuteScalarAsync<long>(sql, student);
        }

        public async Task<Student?> GetByIdAsync(long id)
        {
            const string sql = @"
            SELECT id, email, first_name AS FirstName, last_name AS LastName, company
            FROM bagile.students
            WHERE id = @Id;";

            await using var c = new NpgsqlConnection(_conn);
            return await c.QuerySingleOrDefaultAsync<Student>(sql, new { Id = id });
        }
    }

}