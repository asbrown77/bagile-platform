using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.Domain.Entities
{
    public class CourseDefinition
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationDays { get; set; } = 2;
        public decimal? BasePrice { get; set; }
        public bool Active { get; set; } = true;
    }

}
