using Bagile.Application.CourseSchedules.DTOs;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.CreatePrivateCourse;

public record CreatePrivateCourseCommand : IRequest<CourseScheduleDetailDto>
{
    public string Name { get; init; } = "";
    public string CourseCode { get; init; } = "";
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string FormatType { get; init; } = "virtual";
    public string? TrainerName { get; init; }
    public int? Capacity { get; init; }
    public decimal? Price { get; init; }
    public long? ClientOrganisationId { get; init; }
    public string? Notes { get; init; }
    public string? InvoiceReference { get; init; }
    public string? MeetingUrl { get; init; }
    public string? MeetingId { get; init; }
    public string? MeetingPasscode { get; init; }
    public string? VenueAddress { get; init; }
}
