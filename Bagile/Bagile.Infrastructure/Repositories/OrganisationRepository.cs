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

    public async Task<OrgConfigDto?> GetConfigByNameAsync(string name, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, name, aliases, primary_domain, partner_type, ptn_tier, discount_rate, contact_email
            FROM bagile.organisations
            WHERE name ILIKE @name
               OR LOWER(TRIM(@name)) = ANY(SELECT LOWER(TRIM(a)) FROM UNNEST(aliases) a)
            LIMIT 1;";

        await using var conn = new NpgsqlConnection(_connStr);
        var row = await conn.QuerySingleOrDefaultAsync(sql, new { name });
        if (row == null) return null;

        var aliasArray = (row.aliases as string[]) ?? Array.Empty<string>();
        return new OrgConfigDto
        {
            Id            = (long)row.id,
            Name          = (string)row.name,
            Aliases       = aliasArray.ToList(),
            PrimaryDomain = (string?)row.primary_domain,
            PartnerType   = (string?)row.partner_type,
            PtnTier       = (string?)row.ptn_tier,
            DiscountRate  = (decimal?)row.discount_rate,
            ContactEmail  = (string?)row.contact_email,
        };
    }

    public async Task<OrgConfigDto?> UpdateConfigAsync(long id, List<string> aliases, string? primaryDomain, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE bagile.organisations
            SET aliases        = @aliases::TEXT[],
                primary_domain = @primaryDomain,
                updated_at     = NOW()
            WHERE id = @id
            RETURNING name;";

        await using var conn = new NpgsqlConnection(_connStr);
        var name = await conn.QuerySingleOrDefaultAsync<string>(sql, new
        {
            id,
            aliases        = aliases.ToArray(),
            primaryDomain,
        });

        if (name == null) return null;
        return await GetConfigByNameAsync(name, ct);
    }
}
