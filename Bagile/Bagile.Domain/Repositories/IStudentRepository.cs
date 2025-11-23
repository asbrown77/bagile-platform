using Bagile.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.Domain.Repositories
{
    public interface IStudentRepository
    {
        Task<long> UpsertAsync(Student student);
        Task<Student?> GetByIdAsync(long id);
    }
}
