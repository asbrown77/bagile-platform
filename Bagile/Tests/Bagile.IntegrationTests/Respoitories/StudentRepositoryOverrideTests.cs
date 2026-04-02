using System.Text.Json;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.Infrastructure.Repositories;
using Dapper;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace Bagile.IntegrationTests.Respoitories;

/// <summary>
/// Integration tests for student override + ETL interaction — Sprint 21 item 10.
///
/// Covers the invariant: once a field is flagged as overridden, ETL upserts must
/// leave that field unchanged even if they supply a different value.
/// Non-overridden fields must update normally.
/// </summary>
[TestFixture]
[Category("Integration")]
public class StudentRepositoryOverrideTests
{
    private StudentRepository _repo = null!;
    private string _connStr = null!;

    [SetUp]
    public async Task Setup()
    {
        _connStr = DatabaseFixture.ConnectionString;
        _repo = new StudentRepository(_connStr);

        // Remove test rows before each test
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(
            "DELETE FROM bagile.students WHERE email LIKE '%@override-test.example%'");
    }

    // ── Core override invariant ──────────────────────────────────────────────

    [Test]
    public async Task Upsert_Preserves_Overridden_Email_On_Re_Sync()
    {
        // 1. Insert a student with a WooCommerce email
        var wooEmail    = "woo-original@override-test.example";
        var correctEmail = "correct@override-test.example";

        var studentId = await _repo.UpsertAsync(new Student
        {
            Email     = wooEmail,
            FirstName = "Jo",
            LastName  = "Bloggs"
        });

        // 2. Trainer corrects the email via portal override
        await _repo.OverrideAsync(studentId, new StudentOverride
        {
            Email     = correctEmail,
            UpdatedBy = "alexbrown@bagile.co.uk",
        });

        // 3. ETL re-syncs with the original (wrong) email
        await _repo.UpsertAsync(new Student
        {
            Email     = wooEmail,      // ETL still has old email — should be ignored
            FirstName = "Jo",
            LastName  = "Bloggs"
        });

        // 4. The overridden email must be preserved
        var saved = await _repo.GetByIdAsync(studentId);
        saved.Should().NotBeNull();
        saved!.Email.Should().Be(correctEmail,
            because: "the email was manually overridden and ETL must not overwrite it");
    }

    [Test]
    public async Task Upsert_Updates_NonOverridden_Fields_Normally()
    {
        // Insert a student — no overrides
        var email = "no-override@override-test.example";
        var studentId = await _repo.UpsertAsync(new Student
        {
            Email     = email,
            FirstName = "Jo",
            LastName  = "Bloggs",
            Company   = "Acme Corp"
        });

        // ETL re-syncs with updated details (company has changed)
        await _repo.UpsertAsync(new Student
        {
            Email     = email,
            FirstName = "Joanna",       // updated
            LastName  = "Bloggs",
            Company   = "New Company"  // updated
        });

        var saved = await _repo.GetByIdAsync(studentId);
        saved.Should().NotBeNull();
        saved!.FirstName.Should().Be("Joanna",     because: "first_name was not overridden");
        saved!.Company.Should().Be("New Company",  because: "company was not overridden");
    }

    [Test]
    public async Task Override_FirstName_Survives_ETL_Upsert_With_Different_Value()
    {
        var email = "fname-override@override-test.example";
        var studentId = await _repo.UpsertAsync(new Student
        {
            Email     = email,
            FirstName = "Jonathan",
            LastName  = "Smith"
        });

        // Portal corrects first name (e.g. "Jonathan" → "Jon")
        await _repo.OverrideAsync(studentId, new StudentOverride
        {
            FirstName = "Jon",
            UpdatedBy = "portal"
        });

        // ETL syncs again with old value
        await _repo.UpsertAsync(new Student
        {
            Email     = email,
            FirstName = "Jonathan",
            LastName  = "Smith"
        });

        var saved = await _repo.GetByIdAsync(studentId);
        saved!.FirstName.Should().Be("Jon",
            because: "first_name was overridden and must survive ETL re-sync");
    }

    [Test]
    public async Task Override_Sets_OverriddenFields_Flag_Correctly()
    {
        var email = "flags@override-test.example";
        var studentId = await _repo.UpsertAsync(new Student
        {
            Email     = email,
            FirstName = "Test",
            LastName  = "User"
        });

        await _repo.OverrideAsync(studentId, new StudentOverride
        {
            FirstName = "Override",
            UpdatedBy = "test"
        });

        var saved = await _repo.GetByIdAsync(studentId);
        saved!.OverriddenFields.Should().NotBeNullOrEmpty();

        // OverriddenFields is stored as a JSON string in the domain entity
        var flags = JsonDocument.Parse(saved!.OverriddenFields!).RootElement;
        flags.TryGetProperty("first_name", out var fnFlag).Should().BeTrue(
            because: "first_name was overridden so the flag must be present");
        fnFlag.GetBoolean().Should().BeTrue();
    }

    [Test]
    public async Task Mixed_Overridden_And_Normal_Fields_Are_Handled_Independently()
    {
        // Company is overridden; last_name is not.
        var email = "mixed@override-test.example";
        var studentId = await _repo.UpsertAsync(new Student
        {
            Email     = email,
            FirstName = "Alex",
            LastName  = "Brown",
            Company   = "b-agile"
        });

        // Override company (e.g. PTN partner email corrected)
        await _repo.OverrideAsync(studentId, new StudentOverride
        {
            Company   = "QA Ltd",
            UpdatedBy = "alexbrown@bagile.co.uk"
        });

        // ETL re-syncs: company still shows "b-agile", last_name changed
        await _repo.UpsertAsync(new Student
        {
            Email     = email,
            FirstName = "Alex",
            LastName  = "Brown-Updated",
            Company   = "b-agile"
        });

        var saved = await _repo.GetByIdAsync(studentId);
        saved!.Company.Should().Be("QA Ltd",
            because: "company was overridden");
        saved!.LastName.Should().Be("Brown-Updated",
            because: "last_name was not overridden so ETL update applies");
    }
}
