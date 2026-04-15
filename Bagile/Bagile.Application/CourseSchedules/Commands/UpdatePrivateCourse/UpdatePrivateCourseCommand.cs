using Bagile.Application.CourseSchedules.DTOs;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.UpdatePrivateCourse;

public record UpdatePrivateCourseCommand : IRequest<CourseScheduleDetailDto?>
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string? CourseCode { get; init; }
    public string? TrainerName { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int? Capacity { get; init; }
    public decimal? Price { get; init; }
    public long? ClientOrganisationId { get; init; }
    public string? InvoiceReference { get; init; }
    public string? VenueAddress { get; init; }
    public string? MeetingUrl { get; init; }
    public string? MeetingId { get; init; }
    public string? MeetingPasscode { get; init; }
    public string? Notes { get; init; }
    /// <summary>Optional status override — confirmed, planned, cancelled etc.</summary>
    public string? Status { get; init; }
}
