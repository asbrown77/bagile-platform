using Bagile.EtlService;
using Bagile.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<IRawOrderRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connStr = config.GetConnectionString("DefaultConnection") 
                  ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
                  ?? config.GetValue<string>("DbConnectionString");
    return new RawOrderRepository(connStr!);
});

builder.Services.AddEndpointsApiExplorer();

var host = builder.Build();

host.MapPost("/webhooks/woo", async (HttpContext http, IRawOrderRepository repo) =>
{
    using var reader = new StreamReader(http.Request.Body);
    var body = await reader.ReadToEndAsync();

    string externalId;
    try
    {
        using var doc = JsonDocument.Parse(body);
        externalId = doc.RootElement.GetProperty("id").GetRawText().Trim('"');
    }
    catch
    {
        http.Response.StatusCode = 400;
        await http.Response.WriteAsync("Invalid WooCommerce payload: missing 'id'");
        return;
    }

    var id = await repo.UpsertAsync("woo", externalId, body);
    await http.Response.WriteAsJsonAsync(new { id });
});

host.MapGet("/debug/raw_orders", async (IRawOrderRepository repo) =>
{
    var all = await repo.GetAllAsync();
    return Results.Json(all.Take(10));
});

host.Run();
