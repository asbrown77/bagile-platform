using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.AcceptanceTests
{
    // Test Data Models
    public class OrderTestData
    {
        public string ExternalId { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerCompany { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class EnrolmentTestData
    {
        public string StudentEmail { get; set; } = "";
        public string CourseName { get; set; } = "";
    }

    public class OrderQueryParameters
    {
        public string? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Email { get; set; }
    }

    public class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
    }
}
