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

        /// <summary>
        /// Manually override student fields from the portal.
        /// Marks the updated fields in overridden_fields so ETL does not overwrite them.
        /// </summary>
        Task<Student?> OverrideAsync(long id, StudentOverride @override, CancellationToken ct = default);
    }

    /// <summary>
    /// Fields that can be manually corrected on a student record.
    /// Null values are left unchanged.
    /// </summary>
    public class StudentOverride
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Company { get; set; }
        public string? UpdatedBy { get; set; }
        public string? OverrideNote { get; set; }
    }
}
