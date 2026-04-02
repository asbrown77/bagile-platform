using Bagile.Application.Common.Interfaces;
using Bagile.Application.Organisations.DTOs;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class OrganisationRepository : IOrganisationRepository
{
    private readonly string _connStr;

    public OrganisationRepository(string connStr)
    {
        _connStr = connStr;
    }

    public async Task<OrganisationSummaryDto> CreateAsync(string name, string? acronym, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO bagile.organisations (name, acronym, aliases)
            VALUES (@name, @acronym, ARRAY[@name]::TEXT[])
            RETURNING id, name, acronym, partner_type AS PartnerType, ptn_tier AS PtnTier;";

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleAsync<OrganisationSummaryDto>(sql, new { name, acronym });
    }
}
