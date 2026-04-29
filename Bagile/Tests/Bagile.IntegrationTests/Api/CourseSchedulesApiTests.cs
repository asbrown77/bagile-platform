using System.Net.Http.Json;
using Bagile.Application.Common.Models;
using Bagile.Application.CourseSchedules.DTOs;
using Dapper;
using FluentAssertions;
using NUnit.Framework;

namespace Bagile.IntegrationTests.Api;

[TestFixture]
public class CourseSchedulesApiTests : IntegrationTestBase
{
    [Test]
    public async Task GET_CourseSchedules_Should_Return_Paginated_Results()
    {
        // Arrange: Create test data
        var scheduleId1 = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM - London Jan 2025', 'PSM', 'published', '2025-01-15', true)
            RETURNING id;
        ");

        var scheduleId2 = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSPO - Manchester Feb 2025', 'PSPO', 'published', '2025-02-10', true)
            RETURNING id;
        ");

        // Act
        var response = await _client.GetAsync("/api/course-schedules");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CourseScheduleDto>>();

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.TotalPages.Should().Be(1);
    }

    [Test]
    public async Task GET_CourseSchedules_Should_Filter_By_CourseCode()
    {
        // Arrange
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES 
                ('PSM - London', 'PSM', 'published', '2025-01-15', true),
                ('PSPO - London', 'PSPO', 'published', '2025-02-10', true);
        ");

        // Act
        var response = await _client.GetAsync("/api/course-schedules?courseCode=PSM");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CourseScheduleDto>>();
        result!.Items.Should().HaveCount(1);
        result.Items.First().CourseCode.Should().Be("PSM");
    }

    [Test]
    public async Task GET_CourseSchedules_Should_Calculate_GuaranteedToRun()
    {
        // Arrange: Create a course with 3+ enrolments
        var scheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM - London', 'PSM', 'published', '2025-01-15', true)
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES ('ORD-001', 'woo', 'public', 'completed', 1500, NOW())
            RETURNING id;
        ");

        // Add 3 students/enrolments
        for (int i = 1; i <= 3; i++)
        {
            var studentId = await _db.ExecuteScalarAsync<long>(@"
                INSERT INTO bagile.students (email, first_name, last_name)
                VALUES (@email, @firstName, @lastName)
                RETURNING id;
            ", new { email = $"student{i}@test.com", firstName = $"Student{i}", lastName = "Test" });

            await _db.ExecuteAsync(@"
                INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
                VALUES (@studentId, @orderId, @scheduleId);
            ", new { studentId, orderId, scheduleId });
        }

        // Act
        var response = await _client.GetAsync($"/api/course-schedules/{scheduleId}");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<CourseScheduleDetailDto>();
        result!.GuaranteedToRun.Should().BeTrue();
        result.CurrentEnrolmentCount.Should().Be(3);
    }

    [Test]
    public async Task GET_CourseScheduleAttendees_Should_Return_Enrolled_Students()
    {
        // Arrange
        var scheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM - London', 'PSM', 'published', '2025-01-15', true)
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES ('ORD-001', 'woo', 'public', 'completed', 500, NOW())
            RETURNING id;
        ");

        var studentId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name, company)
            VALUES ('john.doe@test.com', 'John', 'Doe', 'Test Corp')
            RETURNING id;
        ");

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
            VALUES (@studentId, @orderId, @scheduleId);
        ", new { studentId, orderId, scheduleId });

        // Act
        var response = await _client.GetAsync($"/api/course-schedules/{scheduleId}/attendees");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<CourseAttendeeDto>>();
        result.Should().HaveCount(1);
        result!.First().Email.Should().Be("john.doe@test.com");
        result.First().Organisation.Should().Be("Test Corp");
    }

    [Test]
    public async Task GET_CourseScheduleById_Should_Return_404_For_NonExistent_Schedule()
    {
        // Act
        var response = await _client.GetAsync("/api/course-schedules/99999");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task POST_CancelSchedule_Should_Mark_Active_Enrolments_As_PendingTransfer()
    {
        // Arrange — course with two active enrolments and one already-transferred one
        var scheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSPO-AI - Cancel test', 'PSPOAI-CANCELTEST-CB', 'publish', '2026-06-01', true)
            RETURNING id;
        ");

        var studentA = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name)
            VALUES ('cancel-a@test.com', 'Active', 'A')
            RETURNING id;
        ");
        var studentB = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name)
            VALUES ('cancel-b@test.com', 'Active', 'B')
            RETURNING id;
        ");
        var studentC = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name)
            VALUES ('cancel-c@test.com', 'Already', 'Transferred')
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES ('ORD-CANCELTEST', 'woo', 'public', 'completed', 1650, NOW())
            RETURNING id;
        ");

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id, status)
            VALUES (@studentA, @orderId, @scheduleId, 'active'),
                   (@studentB, @orderId, @scheduleId, 'active'),
                   (@studentC, @orderId, @scheduleId, 'transferred');
        ", new { studentA, studentB, studentC, orderId, scheduleId });

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/course-schedules/{scheduleId}/cancel",
            new { reason = "force_majeure_test" });

        // Assert — endpoint succeeded
        response.EnsureSuccessStatusCode();

        // Course flipped to cancelled
        var courseStatus = await _db.ExecuteScalarAsync<string>(
            "SELECT status FROM bagile.course_schedules WHERE id = @scheduleId;",
            new { scheduleId });
        courseStatus.Should().Be("cancelled");

        // Both active enrolments are now pending_transfer with provider_cancelled reason
        var enrolmentRows = (await _db.QueryAsync<(long student_id, string status, string? cancellation_reason, bool? refund_eligible)>(
            @"SELECT student_id, status, cancellation_reason, refund_eligible
              FROM bagile.enrolments
              WHERE course_schedule_id = @scheduleId
              ORDER BY student_id;",
            new { scheduleId })).ToList();

        enrolmentRows.Should().HaveCount(3);

        var rowA = enrolmentRows.Single(r => r.student_id == studentA);
        rowA.status.Should().Be("pending_transfer");
        rowA.cancellation_reason.Should().Be("provider_cancelled");
        rowA.refund_eligible.Should().BeTrue();

        var rowB = enrolmentRows.Single(r => r.student_id == studentB);
        rowB.status.Should().Be("pending_transfer");

        // Already-transferred enrolment must NOT be touched
        var rowC = enrolmentRows.Single(r => r.student_id == studentC);
        rowC.status.Should().Be("transferred");

        // And the pending ones now appear on /transfers
        var pendingResponse = await _client.GetAsync("/api/transfers/pending");
        pendingResponse.EnsureSuccessStatusCode();
        var pending = await pendingResponse.Content.ReadFromJsonAsync<IEnumerable<Bagile.Application.Transfers.DTOs.PendingTransferDto>>();
        pending.Should().Contain(p => p.StudentId == studentA);
        pending.Should().Contain(p => p.StudentId == studentB);
        pending.Should().NotContain(p => p.StudentId == studentC);
    }
}