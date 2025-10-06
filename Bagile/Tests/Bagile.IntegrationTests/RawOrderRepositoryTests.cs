using System.Linq;
using System.Threading.Tasks;
using Bagile.Infrastructure.Repositories;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace Bagile.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class RawOrderRepositoryTests
{
    private RawOrderRepository _repo = null!;
    private string _connStr = null!;

    [SetUp]
    public async Task Setup()
    {
        _connStr = $"{DatabaseFixture.ConnectionString};SearchPath=bagile";
        _repo = new RawOrderRepository(_connStr);

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM raw_orders WHERE source='woo' AND external_id='123';", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task InsertAsync_ShouldInsert_AndNotThrow()
    {
        var id = await _repo.InsertAsync("woo", "123", """{"id":123}""", "etl.test");
        id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task InsertAsync_Twice_ShouldInsertTwoVersions()
    {
        var first = await _repo.InsertAsync("woo", "123", """{"id":123}""", "etl.test");
        var second = await _repo.InsertAsync("woo", "123", """{"id":123,"changed":true}""", "etl.test");

        second.Should().BeGreaterThan(first);

        var all = await _repo.GetAllAsync();
        var forId = all.Where(x => x.ExternalId == "123").ToList();

        forId.Should().HaveCountGreaterThanOrEqualTo(2, "because we inserted the same order twice");

        var latest = forId.First();
        latest.Payload.Should().Contain("changed", "because the second insert had an updated payload");


    }
}