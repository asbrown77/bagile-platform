using Bagile.Application.Common.Interfaces;
using Bagile.Application.Common.Models;
using Bagile.Application.Orders.DTOs;
using Bagile.Domain.Repositories;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using Bagile.Infrastructure.Persistence.Queries;
using Bagile.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Bagile.IntegrationTests.Api;

[TestFixture]
[Category("Integration")]
public class OrdersApiTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private NpgsqlConnection _db;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connStr = DatabaseFixture.ConnectionString;

        _db = new NpgsqlConnection(connStr);
        await _db.OpenAsync();

        _factory = TestApiFactory.Create(connStr);
        _client = _factory.CreateClient();
    }

    [SetUp]
    public async Task Setup()
    {
        // Clean test data before each test
        await _db.ExecuteAsync(@"
            DELETE FROM bagile.enrolments;
            DELETE FROM bagile.orders;
            DELETE FROM bagile.students;
            DELETE FROM bagile.raw_orders;
        ");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _db?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GET_Orders_Should_Return_200_With_Empty_List()
    {
        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task GET_Orders_Should_Return_Created_Orders()
    {
        // Arrange: Insert test order
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date, contact_name, contact_email, billing_company)
            VALUES ('12243', 'woo', 'public', 'completed', 2520, '2025-10-23', 'Henry Heselden', 'henry@themdu.com', 'MDU Services Ltd');
        ");

        // Act
        var response = await _client.GetAsync("/api/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);

        var order = result.Items.First();
        order.ExternalId.Should().Be("12243");
        order.Status.Should().Be("completed");
        order.TotalAmount.Should().Be(2520);
        order.CustomerName.Should().Be("Henry Heselden");
    }

    [Test]
    public async Task GET_Orders_Should_Filter_By_Status()
    {
        // Arrange
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES 
                ('1', 'woo', 'public', 'completed', 100, NOW()),
                ('2', 'woo', 'public', 'pending', 200, NOW()),
                ('3', 'woo', 'public', 'completed', 300, NOW());
        ");

        // Act
        var response = await _client.GetAsync("/api/orders?status=completed");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(o => o.Status == "completed");
    }

    [Test]
    public async Task GET_Orders_Should_Filter_By_Date_Range()
    {
        // Arrange
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES 
                ('1', 'woo', 'public', 'completed', 100, '2024-01-15'),
                ('2', 'woo', 'public', 'completed', 200, '2024-06-15'),
                ('3', 'woo', 'public', 'completed', 300, '2024-12-15');
        ");

        // Act
        var response = await _client.GetAsync("/api/orders?from=2024-06-01&to=2024-12-01");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        result.Items.Should().HaveCount(1);
        result.Items.First().ExternalId.Should().Be("2");
    }

    [Test]
    public async Task GET_Orders_Should_Support_Pagination()
    {
        // Arrange: Create 25 orders
        for (int i = 1; i <= 25; i++)
        {
            await _db.ExecuteAsync(@"
                INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
                VALUES (@id, 'woo', 'public', 'completed', 100, NOW());
            ", new { id = i.ToString() });
        }

        // Act: Get page 1
        var page1Response = await _client.GetAsync("/api/orders?page=1&pageSize=10");
        var page1 = await page1Response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();

        // Act: Get page 2
        var page2Response = await _client.GetAsync("/api/orders?page=2&pageSize=10");
        var page2 = await page2Response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();

        // Assert
        page1.Items.Should().HaveCount(10);
        page1.TotalCount.Should().Be(25);
        page1.TotalPages.Should().Be(3);
        page1.HasNextPage.Should().BeTrue();

        page2.Items.Should().HaveCount(10);
        page2.Page.Should().Be(2);
    }

    [Test]
    public async Task GET_OrderById_Should_Return_Order_With_Enrolments()
    {
        // Arrange
        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date, contact_email)
            VALUES ('12243', 'woo', 'public', 'completed', 2520, NOW(), 'henry@themdu.com')
            RETURNING id;
        ");

        var studentId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name, company)
            VALUES ('khalil@themdu.com', 'Khalil', 'Nazir', 'MDU Services')
            RETURNING id;
        ");

        var courseId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, status, start_date, source_system, source_product_id)
            VALUES ('PSM Advanced', 'published', '2025-11-08', 'woo', 11840)
            RETURNING id;
        ");

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
            VALUES (@studentId, @orderId, @courseId);
        ", new { studentId, orderId, courseId });

        // Act
        var response = await _client.GetAsync($"/api/orders/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>();
        order.Should().NotBeNull();
        order.Id.Should().Be(orderId);
        order.Enrolments.Should().HaveCount(1);

        var enrolment = order.Enrolments.First();
        enrolment.StudentEmail.Should().Be("khalil@themdu.com");
        enrolment.CourseName.Should().Be("PSM Advanced");
    }

    [Test]
    public async Task GET_OrderById_Should_Return_404_When_Not_Found()
    {
        // Act
        var response = await _client.GetAsync("/api/orders/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GET_Orders_Should_Filter_By_Email()
    {
        // Arrange
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date, contact_email)
            VALUES 
                ('1', 'woo', 'public', 'completed', 100, NOW(), 'alice@example.com'),
                ('2', 'woo', 'public', 'completed', 200, NOW(), 'bob@example.com'),
                ('3', 'woo', 'public', 'completed', 300, NOW(), 'alice@example.com');
        ");

        // Act
        var response = await _client.GetAsync("/api/orders?email=alice@example.com");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(o => o.CustomerEmail == "alice@example.com");
    }
}