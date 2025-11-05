using Npgsql;
using Testcontainers.PostgreSql;

namespace Bagile.IntegrationTests
{
    [SetUpFixture]
    public class DatabaseFixture
    {
        private PostgreSqlContainer _postgres;

        public static string ConnectionString { get; private set; }

        [OneTimeSetUp]
        public async Task GlobalSetup()
        {
            _postgres = new PostgreSqlBuilder()
                .WithDatabase("bagile")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgres.StartAsync();
            ConnectionString = _postgres.GetConnectionString();

            await ApplySchemaAsync();
        }

        private async Task ApplySchemaAsync()
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var repoRoot = Path.GetFullPath(
                Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../..")
            );
            var scriptsPath = Path.Combine(repoRoot, "migrations");

            foreach (var file in Directory.GetFiles(scriptsPath, "*.sql")
                         .OrderBy(f =>
                             int.Parse(Path.GetFileName(f)
                                 .Split('_')[0]
                                 .TrimStart('V'))))
            {
                var sql = await File.ReadAllTextAsync(file);
                await using var cmd = new NpgsqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            if (_postgres != null)
            {
                await _postgres.DisposeAsync();
            }
        }
    }
}