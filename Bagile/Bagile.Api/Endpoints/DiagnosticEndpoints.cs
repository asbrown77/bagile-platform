using Bagile.Infrastructure.Repositories;
using Npgsql;

namespace Bagile.Api.Endpoints;

public static class DiagnosticEndpoints
{
    public static void MapDiagnosticEndpoints(this WebApplication app)
    {
        // DB test endpoint with host logging
        app.MapGet("/dbtest", async (IConfiguration config) =>
        {
            var connStr = config.GetConnectionString("DefaultConnection")
                          ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                          ?? config.GetValue<string>("DbConnectionString");

            if (string.IsNullOrWhiteSpace(connStr))
                return Results.Problem("❌ No connection string found in configuration.");

            try
            {
                // Parse connection string to extract host
                var builder = new NpgsqlConnectionStringBuilder(connStr);
                var host = builder.Host;

                using var conn = new NpgsqlConnection(connStr);
                await conn.OpenAsync();

                return Results.Ok($"✅ Connected successfully to {host}");
            }
            catch (Exception ex)
            {
                var builder = new NpgsqlConnectionStringBuilder(connStr);
                var host = builder.Host;

                return Results.Problem($"❌ Failed. Host: {host} | Error: {ex.Message}");
            }
        });


        app.MapGet("/debug/raw_orders", async (IRawOrderRepository repo, int? limit) =>
        {
            var all = await repo.GetAllAsync();

            // default 10, allow up to 100 max
            var safeLimit = Math.Clamp(limit ?? 10, 1, 100);

            var result = all
                .OrderByDescending(r => r.ReceivedAt)
                .Take(safeLimit);

            return Results.Json(result);
        });

        app.MapHealthChecks("/health");
    }
}