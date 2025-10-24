using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.Domain.Entities
{
    public class Enrolment
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public long OrderId { get; set; }
        public long? CourseScheduleId { get; set; }   
    }
}
