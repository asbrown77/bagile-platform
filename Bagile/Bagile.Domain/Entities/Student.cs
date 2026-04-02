using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.Domain.Entities
{
    public class Student
    {
        public long Id { get; set; }
        public string Email { get; set; } = "";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Company { get; set; }
        public string? Country { get; set; }

        /// <summary>
        /// JSONB map of field names that have been manually overridden.
        /// ETL checks this before writing each field so manual corrections survive re-sync.
        /// Example: {"email": true, "first_name": true}
        /// </summary>
        public string? OverriddenFields { get; set; }

        public string? UpdatedBy { get; set; }
        public string? OverrideNote { get; set; }
    }
}
