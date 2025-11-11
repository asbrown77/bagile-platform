using Bagile.Domain.Entities;
using Bagile.Infrastructure.Repositories;
using Dapper;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace Bagile.IntegrationTests.Repositories;

[TestFixture]
[Category("Integration")]
public class SyncMetadataRepositoryTests : IDisposable
{
    private SyncMetadataRepository _repository = null!;
    private string _connectionString = null!;

    [SetUp]
    public async Task Setup()
    {
        _connectionString = $"{DatabaseFixture.ConnectionString};SearchPath=bagile";
        _repository = new SyncMetadataRepository(_connectionString);

        // Clean up after each test
        await CleanTableAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        // Clean up after each test
        await CleanTableAsync();
    }

    private async Task CleanTableAsync()
    {
        const string sql = "DELETE FROM bagile.sync_metadata;";
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(sql);
    }


    [Test]
    public async Task RecordSyncSuccess_Should_InsertNewRecord()
    {
        // Arrange
        const string source = "woo";
        const string entityType = "products";
        const int recordsSynced = 150;

        // Act
        await _repository.RecordSyncSuccessAsync(source, entityType, recordsSynced);

        // Assert
        var metadata = await _repository.GetSyncMetadataAsync(source, entityType);
        metadata.Should().NotBeNull();
        metadata!.Source.Should().Be(source);
        metadata.EntityType.Should().Be(entityType);
        metadata.RecordsSynced.Should().Be(recordsSynced);
        metadata.SyncStatus.Should().Be("success");
        metadata.ErrorMessage.Should().BeNull();
    }

    [Test]
    public async Task RecordSyncSuccess_Should_UpdateExistingRecord()
    {
        // Arrange
        const string source = "woo";
        const string entityType = "products";

        await _repository.RecordSyncSuccessAsync(source, entityType, 100);
        await Task.Delay(100); // Ensure different timestamp

        // Act - Record another success
        await _repository.RecordSyncSuccessAsync(source, entityType, 200);

        // Assert
        var metadata = await _repository.GetSyncMetadataAsync(source, entityType);
        metadata.Should().NotBeNull();
        metadata!.RecordsSynced.Should().Be(200, "it should update to the latest count");
        metadata.SyncStatus.Should().Be("success");
    }

    [Test]
    public async Task GetLastSuccessfulSyncTime_Should_ReturnNull_WhenNoSyncExists()
    {
        // Arrange
        const string source = "NonExistent";
        const string entityType = "products";

        // Act
        var lastSync = await _repository.GetLastSuccessfulSyncTimeAsync(source, entityType);

        // Assert
        lastSync.Should().BeNull();
    }

    [Test]
    public async Task GetLastSuccessfulSyncTime_Should_ReturnTimestamp_WhenSyncExists()
    {
        // Arrange
        const string source = "woo";
        const string entityType = "products";
        await _repository.RecordSyncSuccessAsync(source, entityType, 50);

        // Act
        var lastSync = await _repository.GetLastSuccessfulSyncTimeAsync(source, entityType);

        // Assert
        lastSync.Should().NotBeNull();
        lastSync.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task RecordSyncFailure_Should_StoreErrorMessage()
    {
        // Arrange
        const string source = "woo";
        const string entityType = "products";
        const string errorMessage = "Connection timeout";

        // Act
        await _repository.RecordSyncFailureAsync(source, entityType, errorMessage);

        // Assert
        var metadata = await _repository.GetSyncMetadataAsync(source, entityType);
        metadata.Should().NotBeNull();
        metadata!.SyncStatus.Should().Be("failed");
        metadata.ErrorMessage.Should().Be(errorMessage);
    }

    [Test]
    public async Task RecordSyncStart_Should_SetStatusToInProgress()
    {
        // Arrange
        const string source = "woo";
        const string entityType = "products";

        // Act
        await _repository.RecordSyncStartAsync(source, entityType);

        // Assert
        var metadata = await _repository.GetSyncMetadataAsync(source, entityType);
        metadata.Should().NotBeNull();
        metadata!.SyncStatus.Should().Be("in_progress");
    }

    [Test]
    public async Task SyncWorkflow_Should_TransitionStates()
    {
        // Arrange
        const string source = "woo";
        const string entityType = "products";

        // Act - Start sync
        await _repository.RecordSyncStartAsync(source, entityType);
        var startMetadata = await _repository.GetSyncMetadataAsync(source, entityType);

        // Act - Complete sync
        await _repository.RecordSyncSuccessAsync(source, entityType, 100);
        var successMetadata = await _repository.GetSyncMetadataAsync(source, entityType);

        // Assert
        startMetadata!.SyncStatus.Should().Be("in_progress");
        successMetadata!.SyncStatus.Should().Be("success");
        successMetadata.RecordsSynced.Should().Be(100);
        successMetadata.ErrorMessage.Should().BeNull();
    }

    [Test]
    public async Task GetLastSuccessfulSyncTime_Should_IgnoreFailedSyncs()
    {
        // Arrange
        const string source = "woo";
        const string entityType = "products";

        // Record a successful sync
        await _repository.RecordSyncSuccessAsync(source, entityType, 100);
        var successTime = await _repository.GetLastSuccessfulSyncTimeAsync(source, entityType);

        // Record a failed sync
        await _repository.RecordSyncFailureAsync(source, entityType, "Some error");

        // Act
        var lastSuccessTime = await _repository.GetLastSuccessfulSyncTimeAsync(source, entityType);

        // Assert - Should return the success time, not the failure time
        lastSuccessTime.Should().Be(successTime);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}