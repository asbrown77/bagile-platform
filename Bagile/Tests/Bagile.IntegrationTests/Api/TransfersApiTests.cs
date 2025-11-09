using System.Net.Http.Json;
using Bagile.Application.Common.Models;
using Bagile.Application.Transfers.DTOs;
using Dapper;
using FluentAssertions;
using NUnit.Framework;

namespace Bagile.IntegrationTests.Api;

[TestFixture]
public class TransfersApiTests : IntegrationTestBase
{
    [Test]
    public async Task GET_Transfers_Should_Detect_Course_Transfers()
    {
        // Arrange: Create a transfer scenario
        var (studentId, originalScheduleId, newScheduleId) = await CreateTransferScenario();

        // Act
        var response = await _client.GetAsync("/api/transfers");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransferDto>>();

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();

        var transfer = result.Items.FirstOrDefault(t => t.StudentId == studentId);
        transfer.Should().NotBeNull();
        transfer!.FromScheduleId.Should().Be(originalScheduleId);
        transfer.ToScheduleId.Should().Be(newScheduleId);
    }

    [Test]
    public async Task GET_Transfers_Should_Identify_CourseCancelled_Reason()
    {
        // Arrange: Transfer due to cancelled course
        var (studentId, _, _) = await CreateTransferScenario(originalStatus: "cancelled");

        // Act
        var response = await _client.GetAsync("/api/transfers");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransferDto>>();
        var transfer = result!.Items.FirstOrDefault(t => t.StudentId == studentId);

        transfer.Should().NotBeNull();
        transfer!.Reason.Should().Be("CourseCancelled");
    }

    [Test]
    public async Task GET_Transfers_Should_Identify_StudentRequest_Reason()
    {
        // Arrange: Transfer with active original course
        var (studentId, _, _) = await CreateTransferScenario(originalStatus: "published");

        // Act
        var response = await _client.GetAsync("/api/transfers");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransferDto>>();
        var transfer = result!.Items.FirstOrDefault(t => t.StudentId == studentId);

        transfer.Should().NotBeNull();
        transfer!.Reason.Should().Be("StudentRequest");
    }

    [Test]
    [Ignore("Fix later not needed")]
    public async Task GET_PendingTransfers_Should_Find_Cancelled_Without_Rebooking()
    {
        // Arrange: Student with cancelled course, no new enrolment
        var studentId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name, company)
            VALUES ('pending@test.com', 'Pending', 'Transfer', 'Test Corp')
            RETURNING id;
        ");

        var cancelledScheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public, last_synced)
            VALUES ('PSM - Cancelled', 'PSM', 'cancelled', '2025-01-15', true, NOW() - INTERVAL '5 days')
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES ('ORD-PENDING', 'woo', 'public', 'completed', 500, NOW())
            RETURNING id;
        ");

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
            VALUES (@studentId, @orderId, @cancelledScheduleId);
        ", new { studentId, orderId, cancelledScheduleId });

        // Act
        var response = await _client.GetAsync("/api/transfers/pending");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<PendingTransferDto>>();

        result.Should().NotBeEmpty();
        var pending = result!.FirstOrDefault(p => p.StudentId == studentId);
        pending.Should().NotBeNull();
        pending!.CourseCode.Should().Be("PSM");
        pending.DaysSinceCancellation.Should().BeGreaterOrEqualTo(5);
    }

    [Test]
    public async Task GET_TransfersByCourse_Should_Show_Inbound_And_Outbound()
    {
        // Arrange
        var middleScheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM - Feb', 'PSM', 'published', '2025-02-15', true)
            RETURNING id;
        ");

        // Student transfers OUT of middle course
        var (student1Id, _, _) = await CreateTransferScenario(
            fromScheduleId: middleScheduleId,
            originalStatus: "published");

        // Student transfers INTO middle course
        var beforeScheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM - Jan', 'PSM', 'cancelled', '2025-01-15', true)
            RETURNING id;
        ");

        var student2Id = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name)
            VALUES ('transfer-in@test.com', 'TransferIn', 'Student')
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES ('ORD-IN', 'woo', 'public', 'completed', 500, NOW())
            RETURNING id;
        ");

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id, created_at)
            VALUES (@student2Id, @orderId, @beforeScheduleId, NOW() - INTERVAL '2 days');
        ", new { student2Id, orderId, beforeScheduleId });

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id, created_at)
            VALUES (@student2Id, @orderId, @middleScheduleId, NOW() - INTERVAL '1 day');
        ", new { student2Id, orderId, middleScheduleId });

        // Act
        var response = await _client.GetAsync($"/api/transfers/by-course/{middleScheduleId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TransfersByCourseDto>();

        result.Should().NotBeNull();
        result!.TotalTransfersOut.Should().BeGreaterThan(0);
        result.TotalTransfersIn.Should().BeGreaterThan(0);
    }

    private async Task<(long studentId, long originalScheduleId, long newScheduleId)> CreateTransferScenario(
        string originalStatus = "cancelled",
        long? fromScheduleId = null)
    {
        var studentId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name, company)
            VALUES (@email, 'Transfer', 'Student', 'Test Corp')
            RETURNING id;
        ", new { email = $"transfer{Guid.NewGuid()}@test.com" });

        var originalScheduleId = fromScheduleId ?? await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM - Original', 'PSM', @status, '2025-01-15', true)
            RETURNING id;
        ", new { status = originalStatus });

        var newScheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM - New', 'PSM', 'published', '2025-02-15', true)
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES (@externalId, 'woo', 'public', 'completed', 500, NOW())
            RETURNING id;
        ", new { externalId = $"ORD-{Guid.NewGuid()}" });

        // Original enrolment
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id, created_at)
            VALUES (@studentId, @orderId, @originalScheduleId, NOW() - INTERVAL '2 days');
        ", new { studentId, orderId, originalScheduleId });

        // Transfer enrolment
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id, created_at)
            VALUES (@studentId, @orderId, @newScheduleId, NOW() - INTERVAL '1 day');
        ", new { studentId, orderId, newScheduleId });

        return (studentId, originalScheduleId, newScheduleId);
    }
}