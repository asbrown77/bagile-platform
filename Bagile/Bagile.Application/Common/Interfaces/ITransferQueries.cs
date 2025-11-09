using Bagile.Application.Transfers.DTOs;

namespace Bagile.Application.Common.Interfaces;

public interface ITransferQueries
{
    /// <summary>
    /// Get paginated list of transfers with filters
    /// </summary>
    Task<IEnumerable<TransferDto>> GetTransfersAsync(
        DateTime? from,
        DateTime? to,
        string? reason,
        string? organisationName,
        long? courseScheduleId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Count total transfers matching filters
    /// </summary>
    Task<int> CountTransfersAsync(
        DateTime? from,
        DateTime? to,
        string? reason,
        string? organisationName,
        long? courseScheduleId,
        CancellationToken ct = default);

    /// <summary>
    /// Get pending transfers (cancelled but not rebooked)
    /// </summary>
    Task<IEnumerable<PendingTransferDto>> GetPendingTransfersAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get transfers for a specific course schedule
    /// </summary>
    Task<TransfersByCourseDto> GetTransfersByCourseAsync(
        long scheduleId,
        CancellationToken ct = default);
}