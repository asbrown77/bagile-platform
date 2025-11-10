using Bagile.Domain.Repositories;
using Npgsql;

namespace Bagile.Api.Endpoints;

public static class DiagnosticEndpoints
{
    public static void MapDiagnosticEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");

    }
}