using Npgsql;
using Testcontainers.PostgreSql;

namespace Bagile.AcceptanceTests;

public static class DatabaseFixture
{
    private static PostgreSqlContainer? _postgres;
    private static readonly SemaphoreSlim Lock = new(1, 1);
    private static bool _initialised;

    public static string ConnectionString { get; private set; } = string.Empty;

    public static async Task EnsureInitializedAsync()
    {
        if (_initialised) return;

        await Lock.WaitAsync();
        try
        {
            if (_initialised) return;

            _postgres = new PostgreSqlBuilder()
                .WithDatabase("bagile")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _postgres.StartAsync();
            ConnectionString = _postgres.GetConnectionString();

            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
            Environment.SetEnvironmentVariable("DbConnectionString", ConnectionString);
            Environment.SetEnvironmentVariable("DATABASE_URL", ConnectionString);

            await ApplySchemaAsync();
            _initialised = true;
        }
        finally
        {
            Lock.Release();
        }
    }

    private static async Task ApplySchemaAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var scriptsPath = FindMigrationsFolder(AppContext.BaseDirectory);

        var migrationFiles = Directory.GetFiles(scriptsPath, "*.sql")
            .OrderBy(f =>
                int.Parse(Path.GetFileName(f)
                    .Split('_')[0]
                    .TrimStart('V')))
            .ToList();

        foreach (var file in migrationFiles)
        {
            var sql = await File.ReadAllTextAsync(file);
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static string FindMigrationsFolder(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);

        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "migrations");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate 'migrations' folder starting from '{startDirectory}'.");
    }

    public static async Task DisposeAsync()
    {
        if (_postgres != null)
        {
            await _postgres.DisposeAsync();
            _postgres = null;
        }
    }
}
