using System.Net.Http.Json;
using Bagile.Application.Common.Models;
using Bagile.Application.Students.DTOs;
using Dapper;
using FluentAssertions;
using NUnit.Framework;

namespace Bagile.IntegrationTests.Api;

[TestFixture]
public class StudentsApiTests : IntegrationTestBase
{
    [Test]
    public async Task GET_Students_Should_Return_All_Students()
    {
        // Arrange
        await CreateTestStudent("john@test.com", "John", "Doe");
        await CreateTestStudent("jane@test.com", "Jane", "Smith");

        // Act
        var response = await _client.GetAsync("/api/students");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<StudentDto>>();

        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Test]
    public async Task GET_Students_Should_Filter_By_Email()
    {
        // Arrange
        await CreateTestStudent("unique@test.com", "Unique", "User");

        // Act
        var response = await _client.GetAsync("/api/students?email=unique@test.com");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<StudentDto>>();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Email.Should().Be("unique@test.com");
    }

    [Test]
    public async Task GET_Students_Should_Filter_By_Name()
    {
        // Arrange
        await CreateTestStudent("search@test.com", "Searchable", "Name");

        // Act
        var response = await _client.GetAsync("/api/students?name=Searchable");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<StudentDto>>();
        result!.Items.Should().Contain(s => s.FirstName == "Searchable");
    }

    [Test]
    public async Task GET_Students_Should_Filter_By_Organisation()
    {
        // Arrange
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.students (email, first_name, last_name, company)
            VALUES ('corp@test.com', 'Corp', 'User', 'Acme Corporation');
        ");

        // Act
        var response = await _client.GetAsync("/api/students?organisation=Acme");

        // Assert
        var result = await response.Content.ReadFromJsonAsync<PagedResult<StudentDto>>();
        result!.Items.Should().Contain(s => s.Company == "Acme Corporation");
    }

    [Test]
    public async Task GET_StudentById_Should_Return_Student_With_Stats()
    {
        // Arrange
        var studentId = await CreateTestStudent("stats@test.com", "Stats", "User");

        // Create an enrolment for this student
        var scheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('Test Course', 'TEST', 'published', '2025-03-01', true)
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES ('ORD-STATS', 'woo', 'public', 'completed', 500, NOW())
            RETURNING id;
        ");

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
            VALUES (@studentId, @orderId, @scheduleId);
        ", new { studentId, orderId, scheduleId });

        // Act
        var response = await _client.GetAsync($"/api/students/{studentId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StudentDetailDto>();

        result.Should().NotBeNull();
        result!.TotalEnrolments.Should().Be(1);
        result.LastCourseDate.Should().NotBeNull();
    }

    [Test]
    public async Task GET_StudentEnrolments_Should_Return_Course_History()
    {
        // Arrange
        var studentId = await CreateTestStudent("history@test.com", "History", "User");

        var scheduleId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, sku, status, start_date, is_public)
            VALUES ('PSM Course', 'PSM', 'published', '2025-03-01', true)
            RETURNING id;
        ");

        var orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders (external_id, source, type, status, total_amount, order_date)
            VALUES ('ORD-HIST', 'woo', 'public', 'completed', 500, NOW())
            RETURNING id;
        ");

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
            VALUES (@studentId, @orderId, @scheduleId);
        ", new { studentId, orderId, scheduleId });

        // Act
        var response = await _client.GetAsync($"/api/students/{studentId}/enrolments");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<StudentEnrolmentDto>>();

        result.Should().HaveCount(1);
        result!.First().CourseCode.Should().Be("PSM");
    }

    [Test]
    public async Task GET_StudentById_Should_Return_404_For_NonExistent_Student()
    {
        // Act
        var response = await _client.GetAsync("/api/students/99999");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    private async Task<long> CreateTestStudent(string email, string firstName, string lastName)
    {
        return await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name)
            VALUES (@email, @firstName, @lastName)
            RETURNING id;
        ", new { email, firstName, lastName });
    }
}