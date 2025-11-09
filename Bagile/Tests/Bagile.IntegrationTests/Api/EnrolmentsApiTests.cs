using System.Net.Http.Json;
using Bagile.Application.Common.Models;
using Bagile.Application.Enrolments.DTOs;
using Dapper;
using FluentAssertions;
using NUnit.Framework;

namespace Bagile.IntegrationTests.Api;

[TestFixture]
public class EnrolmentsApiTests : IntegrationTestBase
{
    [Test]
    public async Task GET_Enrolments_Should_Return_All_Enrolments()
    {
        // Arrange
        var (studentId, orderId, scheduleId) = await CreateTestEnrolment();

        // Act
        var response = await _client.GetAsync("/api/enrolments");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EnrolmentListDto>>();

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Test]
    public async Task GET_Enrolments_Should_Filter_By_StudentId()
    {
        // Arrange
        var (studentId, _, _) = await CreateTestEnrolment();

        // Act
        var response = await _client.GetAsync($"/api/enrolments?studentId={studentId}");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EnrolmentListDto>>();
        result!.Items.Should().HaveCount(1);
        result.Items.First().StudentId.Should().Be(studentId);
    }

    [Test]
    public async Task GET_Enrolments_Should_Filter_By_CourseScheduleId()
    {
        // Arrange
        var (_, _, scheduleId) = await CreateTestEnrolment();

        // Act
        var response = await _client.GetAsync($"/api/enrolments?courseScheduleId={scheduleId}");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EnrolmentListDto>>();
        result!.Items.Should().HaveCount(1);
        result.Items.First().CourseScheduleId.Should().Be(scheduleId);
    }

    [Test]
    public async Task GET_Enrolments_Should_Detect_Transfers()
    {
        // Arrange: Create a transfer scenario
        var studentId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name, company)
            VALUES ('transfer@test.com', 'Transfer', 'Student', 'Test Corp')
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES ('ORD-TRANSFER', 'woo', 'public', 'completed', 500, NOW())
            RETURNING id;
        ");

        // Original course (cancelled)
        var originalScheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM - Jan (Cancelled)', 'PSM', 'cancelled', '2025-01-15', true)
            RETURNING id;
        ");

        // New course (same SKU)
        var newScheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM - Feb (Rescheduled)', 'PSM', 'published', '2025-02-15', true)
            RETURNING id;
        ");

        // Create enrolments (original first, then transfer)
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id, created_at)
            VALUES (@studentId, @orderId, @originalScheduleId, NOW() - INTERVAL '1 day');
        ", new { studentId, orderId, originalScheduleId });

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id, created_at)
            VALUES (@studentId, @orderId, @newScheduleId, NOW());
        ", new { studentId, orderId, newScheduleId });

        // Act
        var response = await _client.GetAsync($"/api/enrolments?studentId={studentId}");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EnrolmentListDto>>();
        var transferEnrolment = result!.Items.FirstOrDefault(e => e.CourseScheduleId == newScheduleId);

        transferEnrolment.Should().NotBeNull();
        transferEnrolment!.IsTransfer.Should().BeTrue();
        transferEnrolment.TransferFromScheduleId.Should().Be(originalScheduleId);
    }

    private async Task<(long studentId, long orderId, long scheduleId)> CreateTestEnrolment()
    {
        var scheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('Test Course', 'TEST', 'published', '2025-03-01', true)
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES ('ORD-TEST', 'woo', 'public', 'completed', 500, NOW())
            RETURNING id;
        ");

        var studentId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name, company)
            VALUES ('test@example.com', 'Test', 'Student', 'Test Corp')
            RETURNING id;
        ");

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
            VALUES (@studentId, @orderId, @scheduleId);
        ", new { studentId, orderId, scheduleId });

        return (studentId, orderId, scheduleId);
    }
}