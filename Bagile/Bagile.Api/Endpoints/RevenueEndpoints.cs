using Dapper;
using Npgsql;

namespace Bagile.Api.Endpoints;

public static class RevenueEndpoints
{
    public static void MapRevenueEndpoints(this WebApplication app)
    {
        app.MapGet("/api/analytics/revenue", GetRevenue);
    }

    private static async Task<IResult> GetRevenue(IConfiguration config)
    {
        var connStr = config.GetConnectionString("DefaultConnection")
            ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");

        await using var conn = new NpgsqlConnection(connStr);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var yearStart = new DateTime(now.Year, 1, 1);

        var result = await conn.QueryFirstAsync<dynamic>(@"
            SELECT
                (SELECT COALESCE(SUM(total_amount), 0) FROM bagile.orders
                 WHERE status = 'completed' AND order_date >= @monthStart) AS month_total,
                (SELECT COALESCE(SUM(total_amount), 0) FROM bagile.orders
                 WHERE status = 'completed' AND order_date >= @yearStart) AS year_total,
                (SELECT COUNT(*) FROM bagile.orders
                 WHERE status = 'completed' AND order_date >= @monthStart) AS month_orders,
                (SELECT COUNT(*) FROM bagile.orders
                 WHERE status = 'completed' AND order_date >= @yearStart) AS year_orders
        ", new { monthStart, yearStart });

        // Monthly breakdown for current year
        var monthly = await conn.QueryAsync<dynamic>(@"
            SELECT
                EXTRACT(MONTH FROM order_date)::int AS month,
                COALESCE(SUM(total_amount), 0) AS total,
                COUNT(*) AS orders
            FROM bagile.orders
            WHERE status = 'completed' AND order_date >= @yearStart
            GROUP BY EXTRACT(MONTH FROM order_date)
            ORDER BY month
        ", new { yearStart });

        return Results.Ok(new
        {
            thisMonth = new { total = (decimal)result.month_total, orders = (long)result.month_orders },
            thisYear = new { total = (decimal)result.year_total, orders = (long)result.year_orders },
            monthlyBreakdown = monthly
        });
    }
}
