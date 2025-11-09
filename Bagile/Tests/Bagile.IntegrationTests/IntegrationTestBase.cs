using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using NUnit.Framework;

namespace Bagile.IntegrationTests;

/// <summary>
/// Base class for API integration tests with database setup/cleanup
/// </summary>
public abstract class IntegrationTestBase
{
    protected WebApplicationFactory<Program> _factory = null!;
    protected HttpClient _client = null!;
    protected NpgsqlConnection _db = null!;

    private const string TestApiKey = "integration-test-api-key";

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connStr = DatabaseFixture.ConnectionString;

        _db = new NpgsqlConnection(connStr);
        await _db.OpenAsync();

        _factory = TestApiFactory.Create(connStr, TestApiKey);
        _client = _factory.CreateClient();

        _client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);
    }

    [SetUp]
    public async Task Setup()
    {
        // Clean test data before each test
        await CleanDatabase();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _db?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
    }

    /// <summary>
    /// Clean all test data from database
    /// </summary>
    protected virtual async Task CleanDatabase()
    {
        await _db.ExecuteAsync(@"
            DELETE FROM bagile.enrolments;
            DELETE FROM bagile.course_schedules;
            DELETE FROM bagile.students;
            DELETE FROM bagile.orders;
            DELETE FROM bagile.raw_orders;
        ");
    }
}