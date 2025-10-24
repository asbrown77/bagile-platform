using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories
{
    public interface IEnrolmentRepository
    {
        Task UpsertAsync(Enrolment enrolment);
    }
}