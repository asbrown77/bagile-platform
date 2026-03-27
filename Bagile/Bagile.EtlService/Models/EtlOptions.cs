namespace Bagile.EtlService.Models;

public class EtlOptions
{
    public const string SectionName = "Etl";
    public int IntervalMinutes { get; set; } = 5;
}
