using Bagile.AcceptanceTests;
using Bagile.AcceptanceTests.Drivers;
using BoDi;
using Microsoft.AspNetCore.Mvc.Testing;
using TechTalk.SpecFlow;

[Binding]
public class AcceptanceTestSetup
{
    private readonly IObjectContainer _container;
    private WebApplicationFactory<Program>? _factory;

    public AcceptanceTestSetup(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        // Start Postgres container and run migrations once.
        // This also sets env vars for the connection string now.
        await DatabaseFixture.EnsureInitializedAsync();
    }

    [BeforeScenario(Order = 0)]
    public async Task CleanDatabaseBeforeScenario()
    {
        // Always ensure a clean database state before each scenario.
        var connStr = DatabaseFixture.ConnectionString;
        if (string.IsNullOrWhiteSpace(connStr))
        {
            throw new InvalidOperationException(
                "DatabaseFixture was not initialised before scenario. " +
                "Check BeforeTestRun / DatabaseFixture.EnsureInitializedAsync."
            );
        }

        var db = new DatabaseDriver(connStr);
        await db.CleanDatabaseAsync();
    }

    [BeforeScenario(Order = 1)]
    public void Setup()
    {
        var connStr = DatabaseFixture.ConnectionString;
        if (string.IsNullOrWhiteSpace(connStr))
        {
            throw new InvalidOperationException(
                "DatabaseFixture was not initialised before scenario. " +
                "Check BeforeTestRun / DatabaseFixture.EnsureInitializedAsync."
            );
        }

        _factory = new WebApplicationFactory<Program>();

        var client = _factory.CreateClient();

        _container.RegisterInstanceAs(new ApiDriver(client));
        _container.RegisterInstanceAs(new DatabaseDriver(connStr));
    }

    [AfterScenario]
    public void Teardown()
    {
        _factory?.Dispose();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        await DatabaseFixture.DisposeAsync();
    }
}
