using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface IEnrolmentRepository
{
    Task UpsertAsync(Enrolment enrolment);
    Task<int> CountByOrderIdAsync(long orderId);

    Task<long> InsertAsync(Enrolment enrolment);

    Task<Enrolment?> FindByOrderStudentAndSkuAsync(
        long orderId,
        long studentId,
        string sku);

    Task UpdateStatusAsync(
        long enrolmentId,
        string status,
        long? transferredToEnrolmentId = null);

    Task<Enrolment?> FindHeuristicTransferSourceAsync(long studentId, string courseFamilyPrefix);
    Task MarkTransferredAsync(long enrolmentId, long transferredToEnrolmentId);
    Task<Enrolment?> FindActiveByStudentEmailAsync(string email);
    Task CancelEnrolmentAsync(long enrolmentId, string? reason = null);
    Task<IEnumerable<Enrolment>> GetByOrderIdAsync(long orderId);

}