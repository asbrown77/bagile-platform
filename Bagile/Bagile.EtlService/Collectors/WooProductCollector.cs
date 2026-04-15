using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Mappers;
using Bagile.Infrastructure.Models;
using Bagile.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Collectors;

/// <summary>
/// Collects WooCommerce products and syncs them as CourseSchedules.
/// Supports full pagination and incremental sync using sync metadata tracking.
/// </summary>
public class WooProductCollector : IProductCollector
{
    private readonly IWooApiClient _wooApiClient;
    private readonly ICourseScheduleRepository _scheduleRepo;
    private readonly IEnrolmentRepository _enrolmentRepo;
    private readonly ICourseDefinitionRepository _definitionRepo;
    private readonly ISyncMetadataRepository _syncMetadataRepo;
    private readonly ILogger<WooProductCollector> _logger;

    private const string SourceName = "woo";
    private const string EntityType = "products";

    string IProductCollector.SourceName => SourceName;

    public WooProductCollector(
        IWooApiClient wooApiClient,
        ICourseScheduleRepository scheduleRepo,
        IEnrolmentRepository enrolmentRepo,
        ICourseDefinitionRepository definitionRepo,
        ISyncMetadataRepository syncMetadataRepo,
        ILogger<WooProductCollector> logger)
    {
        _wooApiClient = wooApiClient;
        _scheduleRepo = scheduleRepo;
        _enrolmentRepo = enrolmentRepo;
        _definitionRepo = definitionRepo;
        _syncMetadataRepo = syncMetadataRepo;
        _logger = logger;
    }

    public async Task CollectProductsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting WooCommerce product sync...");

        try
        {
            // Record sync start
            await _syncMetadataRepo.RecordSyncStartAsync(SourceName, EntityType);

            // Get last successful sync time for incremental sync
            var lastSyncTime = await GetLastProductSyncTimeAsync();

            if (lastSyncTime.HasValue)
            {
                _logger.LogInformation("Performing incremental sync (modified since {LastSync})", lastSyncTime.Value);
            }
            else
            {
                _logger.LogInformation("Performing full sync (first time or no previous sync found)");
            }

            // Fetch products from WooCommerce (with pagination handled by API client)
            var products = await _wooApiClient.FetchProductsAsync(lastSyncTime, ct);

            if (products is null || products.Count == 0)
            {
                _logger.LogWarning("No products returned from WooCommerce.");
                await _syncMetadataRepo.RecordSyncSuccessAsync(SourceName, EntityType, 0);
                return;
            }

            _logger.LogInformation("Processing {Count} products from WooCommerce...", products.Count);

            // Load all course definitions once for performance
            var definitions = (await _definitionRepo.GetAllAsync()).ToList();

            var successCount = 0;
            var errorCount = 0;
            var matchedCount = 0;
            var unmatchedCount = 0;

            foreach (var product in products)
            {
                try
                {
                    // Try to match product to a course definition
                    var def = definitions.FirstOrDefault(d =>
                        product.Name.Contains(d.Code, StringComparison.OrdinalIgnoreCase) ||
                        product.Name.Contains(d.Name, StringComparison.OrdinalIgnoreCase));

                    // Map WooCommerce product to CourseSchedule
                    var schedule = product.ToCourseSchedule();
                    schedule.CourseDefinitionId = def?.Id;
                    schedule.LastSynced = DateTime.UtcNow;

                    // Upsert to database (creates if new, updates if exists)
                    await _scheduleRepo.UpsertAsync(schedule);
                    successCount++;

                    if (def != null)
                    {
                        matchedCount++;
                    }
                    else
                    {
                        unmatchedCount++;
                        _logger.LogDebug(
                            "Product {ProductId} '{ProductName}' could not be matched to a course definition",
                            product.Id, product.Name);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Failed to process Woo product {ProductId} '{ProductName}'",
                        product.Id, product.Name);
                }
            }

            // Record successful sync
            await _syncMetadataRepo.RecordSyncSuccessAsync(SourceName, EntityType, successCount);

            _logger.LogInformation(
                "✅ Product sync complete: {SuccessCount} synced ({MatchedCount} matched to definitions, {UnmatchedCount} unmatched), {ErrorCount} failed",
                successCount, matchedCount, unmatchedCount, errorCount);

            // Orphan cleanup: find portal schedules whose WooCommerce source no longer exists.
            await CleanupOrphanSchedulesAsync(products, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product sync failed");
            await _syncMetadataRepo.RecordSyncFailureAsync(SourceName, EntityType, ex.Message);
            throw;
        }
    }

    // ---------------------------------------------------------------
    // Orphan cleanup
    // ---------------------------------------------------------------

    /// <summary>
    /// For each portal course schedule sourced from WooCommerce, verify the backing
    /// product still exists. If it returns 404 or has been trashed, either hard-delete
    /// (0 enrolments) or mark cancelled (enrolments present).
    ///
    /// We skip schedules whose product ID appears in <paramref name="fetchedProducts"/>
    /// because we just successfully synced them. For everything else we do an individual
    /// API check to distinguish genuine deletion from a transient error.
    /// </summary>
    private async Task CleanupOrphanSchedulesAsync(
        IReadOnlyList<WooProductDto> fetchedProducts,
        CancellationToken ct)
    {
        var fetchedIds = new HashSet<long>(fetchedProducts.Select(p => p.Id));

        IEnumerable<CourseSchedule> wooSchedules;
        try
        {
            wooSchedules = await _scheduleRepo.GetActiveWooSchedulesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orphan cleanup: failed to load active Woo schedules");
            return;
        }

        foreach (var schedule in wooSchedules)
        {
            ct.ThrowIfCancellationRequested();

            if (schedule.SourceProductId is null)
                continue;

            var productId = schedule.SourceProductId.Value;

            // Skip: we just synced this product successfully.
            if (fetchedIds.Contains(productId))
                continue;

            // Check whether the product still exists in WooCommerce.
            bool exists;
            try
            {
                exists = await _wooApiClient.ProductExistsAsync(productId, ct);
            }
            catch (HttpRequestException ex)
            {
                // Non-404 error — treat as transient; skip this schedule for this cycle.
                _logger.LogWarning(
                    ex,
                    "Orphan cleanup: transient error checking product {ProductId} for schedule {ScheduleId} — skipping",
                    productId, schedule.Id);
                continue;
            }

            if (exists)
                continue;

            // Product is gone: decide hard-delete vs cancel.
            int activeEnrolments;
            try
            {
                activeEnrolments = await _enrolmentRepo.CountActiveByScheduleAsync(schedule.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Orphan cleanup: could not count enrolments for schedule {ScheduleId} — skipping",
                    schedule.Id);
                continue;
            }

            if (activeEnrolments == 0)
            {
                _logger.LogInformation(
                    "Orphan cleanup: hard-deleting schedule {ScheduleId} (SKU={Sku}, productId={ProductId}) — source product deleted and 0 enrolments",
                    schedule.Id, schedule.Sku, productId);
                try
                {
                    await _scheduleRepo.DeleteAsync(schedule.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Orphan cleanup: failed to delete schedule {ScheduleId}", schedule.Id);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Orphan cleanup: marking schedule {ScheduleId} (SKU={Sku}) as cancelled — source product deleted but {Count} enrolment(s) exist",
                    schedule.Id, schedule.Sku, activeEnrolments);
                try
                {
                    await _scheduleRepo.UpdateStatusAsync(schedule.Id, "cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Orphan cleanup: failed to cancel schedule {ScheduleId}", schedule.Id);
                }
            }
        }
    }

    /// <summary>
    /// Gets the last successful product sync time for incremental syncing.
    /// Uses the sync_metadata table to track when products were last synced.
    /// </summary>
    private async Task<DateTime?> GetLastProductSyncTimeAsync()
    {
        try
        {
            // Option 1: Use dedicated sync_metadata table (RECOMMENDED)
            var lastSync = await _syncMetadataRepo.GetLastSuccessfulSyncTimeAsync(SourceName, EntityType);

            if (lastSync.HasValue)
            {
                _logger.LogDebug("Last successful product sync: {LastSync}", lastSync.Value);
                return lastSync.Value;
            }

            // Option 2: Fallback to querying course_schedules table (if sync_metadata not available)
            // This is less accurate but works without the sync_metadata table
            _logger.LogDebug("No sync metadata found, checking course_schedules table...");

            // Note: If you want to use this fallback, uncomment the following:
            // var schedules = await _scheduleRepo.GetAllAsync();
            // var maxLastSynced = schedules
            //     .Where(s => s.SourceSystem == "woo" && s.LastSynced.HasValue)
            //     .Max(s => s.LastSynced);
            // 
            // if (maxLastSynced.HasValue)
            // {
            //     _logger.LogDebug("Using MAX(last_synced) from course_schedules: {LastSync}", maxLastSynced.Value);
            //     return maxLastSynced.Value;
            // }

            _logger.LogDebug("No previous sync time found, will perform full sync");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get last sync time, will perform full sync");
            return null;
        }
    }
}