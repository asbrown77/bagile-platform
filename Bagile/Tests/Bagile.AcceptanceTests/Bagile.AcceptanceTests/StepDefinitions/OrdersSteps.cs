using Bagile.AcceptanceTests.Drivers;
using Bagile.Application.Orders.DTOs;
using Bagile.Application.Common.Models;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Bagile.AcceptanceTests.StepDefinitions;

[Binding]
public class OrdersSteps
{
    private readonly ApiDriver _api;
    private readonly DatabaseDriver _database;
    private readonly ScenarioContext _scenarioContext;

    public OrdersSteps(ApiDriver api, DatabaseDriver database, ScenarioContext scenarioContext)
    {
        _api = api;
        _database = database;
        _scenarioContext = scenarioContext;
    }

    [Given(@"the database is clean")]
    public async Task GivenTheDatabaseIsClean()
    {
        await _database.CleanDatabaseAsync();
    }

    [Given(@"the following orders exist:")]
    public async Task GivenTheFollowingOrdersExist(Table table)
    {
        var orders = table.CreateSet<OrderTestData>();
        foreach (var order in orders)
        {
            await _database.InsertOrderAsync(order);
        }
    }

    [Given(@"an order exists with external ID ""(.*)""")]
    public async Task GivenAnOrderExistsWithExternalID(string externalId)
    {
        var orderId = await _database.GetOrderIdByExternalIdAsync(externalId);
        _scenarioContext["OrderId"] = orderId;
    }

    [Given(@"the order has the following enrolments:")]
    public async Task GivenTheOrderHasTheFollowingEnrolments(Table table)
    {
        var orderId = _scenarioContext.Get<long>("OrderId");
        var enrolments = table.CreateSet<EnrolmentTestData>();

        foreach (var enrolment in enrolments)
        {
            await _database.InsertEnrolmentAsync(orderId, enrolment);
        }
    }

    [Given(@"(.*) orders exist in the system")]
    public async Task GivenOrdersExistInTheSystem(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await _database.InsertOrderAsync(new OrderTestData
            {
                ExternalId = i.ToString(),
                Status = "completed",
                TotalAmount = 100.00m,
                OrderDate = DateTime.UtcNow
            });
        }
    }

    [When(@"I request all orders")]
    public async Task WhenIRequestAllOrders() => await _api.GetOrdersAsync();

    [When(@"I request orders with status ""(.*)""")]
    public async Task WhenIRequestOrdersWithStatus(string status)
        => await _api.GetOrdersAsync(status: status);

    [When(@"I request orders from ""(.*)"" to ""(.*)""")]
    public async Task WhenIRequestOrdersFromTo(string from, string to)
        => await _api.GetOrdersAsync(
            from: DateTime.Parse(from),
            to: DateTime.Parse(to));

    [When(@"I request orders for email ""(.*)""")]
    public async Task WhenIRequestOrdersForEmail(string email)
        => await _api.GetOrdersAsync(email: email);

    [When(@"I request the order by its internal ID")]
    public async Task WhenIRequestTheOrderByItsInternalID()
    {
        var orderId = _scenarioContext.Get<long>("OrderId");
        await _api.GetOrderByIdAsync(orderId);
    }

    [When(@"I request order with ID (.*)")]
    public async Task WhenIRequestOrderWithID(long orderId)
        => await _api.GetOrderByIdAsync(orderId);

    [When(@"I request orders with page (.*) and page size (.*)")]
    public async Task WhenIRequestOrdersWithPageAndPageSize(int page, int pageSize)
        => await _api.GetOrdersAsync(page: page, pageSize: pageSize);

    [When(@"I request orders with:")]
    public async Task WhenIRequestOrdersWith(Table table)
    {
        var parameters = table.CreateInstance<OrderQueryParameters>();
        await _api.GetOrdersAsync(
            status: parameters.Status,
            from: parameters.From,
            to: parameters.To,
            email: parameters.Email);
    }

    [Then(@"the response status should be (.*)")]
    public void ThenTheResponseStatusShouldBe(int expectedStatus)
        => _api.LastResponseStatus.Should().Be(expectedStatus);

    [Then(@"the response should contain (.*) orders?")]
    public void ThenTheResponseShouldContainOrders(int expectedCount)
    {
        var result = _api.GetLastPagedResult<OrderDto>();
        result.Items.Count().Should().Be(expectedCount);
    }

    [Then(@"all orders should have status ""(.*)""")]
    public void ThenAllOrdersShouldHaveStatus(string status)
    {
        var result = _api.GetLastPagedResult<OrderDto>();
        result.Items.Should().OnlyContain(o => o.Status == status);
    }

    [Then(@"all orders should be within the date range")]
    public void ThenAllOrdersShouldBeWithinTheDateRange()
    {
        var result = _api.GetLastPagedResult<OrderDto>();
        var from = DateTime.Parse("2025-10-24");
        var to = DateTime.Parse("2025-10-25");

        result.Items.Should().OnlyContain(o =>
            o.OrderDate >= from && o.OrderDate <= to);
    }

    [Then(@"the order should have customer email ""(.*)""")]
    public void ThenTheOrderShouldHaveCustomerEmail(string email)
    {
        var result = _api.GetLastPagedResult<OrderDto>();
        result.Items.First().CustomerEmail.Should().Be(email);
    }

    [Then(@"the order should have:")]
    public void ThenTheOrderShouldHave(Table table)
    {
        var order = _api.GetLastSingleResult<OrderDetailDto>();
        var expected = table.CreateInstance<Dictionary<string, string>>();

        foreach (var kvp in expected)
        {
            var actualValue = order.GetType().GetProperty(kvp.Key)?.GetValue(order)?.ToString();
            actualValue.Should().Be(kvp.Value, $"Property {kvp.Key} should match");
        }
    }

    [Then(@"the order should have (.*) enrolments")]
    public void ThenTheOrderShouldHaveEnrolments(int count)
    {
        var order = _api.GetLastSingleResult<OrderDetailDto>();
        order.Enrolments.Count().Should().Be(count);
    }

    [Then(@"the enrolments should include:")]
    public void ThenTheEnrolmentsShouldInclude(Table table)
    {
        var order = _api.GetLastSingleResult<OrderDetailDto>();
        var expected = table.CreateSet<EnrolmentTestData>();

        foreach (var exp in expected)
        {
            order.Enrolments.Should().Contain(e =>
                e.StudentEmail == exp.StudentEmail &&
                e.CourseName == exp.CourseName);
        }
    }

    [Then(@"the response should contain an error message")]
    public void ThenTheResponseShouldContainAnErrorMessage()
        => _api.LastResponseContent.Should().Contain("error");

    [Then(@"the pagination info should show:")]
    public void ThenThePaginationInfoShouldShow(Table table)
    {
        var result = _api.GetLastPagedResult<OrderDto>();
        var expected = table.CreateInstance<PaginationInfo>();

        result.Page.Should().Be(expected.Page);
        result.PageSize.Should().Be(expected.PageSize);
        result.TotalCount.Should().Be(expected.TotalCount);
        result.TotalPages.Should().Be(expected.TotalPages);
        result.HasNextPage.Should().Be(expected.HasNextPage);
    }

    [Then(@"the order should match all filter criteria")]
    public void ThenTheOrderShouldMatchAllFilterCriteria()
    {
        var result = _api.GetLastPagedResult<OrderDto>();
        result.Items.Count().Should().Be(1);

        var order = result.Items.First();
        order.Status.Should().Be("completed");
        order.CustomerEmail.Should().Be("henry@themdu.com");
    }
}
