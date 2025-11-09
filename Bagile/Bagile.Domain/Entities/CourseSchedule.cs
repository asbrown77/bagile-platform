using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.Domain.Entities;

public class CourseSchedule
{
    public long Id { get; set; }                          // internal DB identity
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? Capacity { get; set; }
    public decimal? Price { get; set; }
    public string? Sku { get; set; }
    public string? TrainerName { get; set; }
    public string? FormatType { get; set; }                // virtual / in_person
    public string? CourseType { get; set; }
    public int? CourseDefinitionId { get; set; }           // FK to course_definitions
    public bool IsPublic { get; set; } = true;
    public string SourceSystem { get; set; } = "woo";
    public long? SourceProductId { get; set; }
    public DateTime LastSynced { get; set; } = DateTime.UtcNow;

    // optional helper for tests
    public CourseSchedule() { }
    public CourseSchedule(long id, string name)
    {
        Id = id;
        Name = name;
    }
}

