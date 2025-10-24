using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.Domain.Repositories
{
    public interface IEnrolmentRepository
    {
        Task UpsertAsync(long studentId, long orderId, long? courseScheduleId);
    }
}
