using Bagile.Application.CourseSchedules.DTOs;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.CreatePrivateCourse;

public record CreatePrivateCourseCommand : IRequest<CourseScheduleDetailDto>
{
    /// <summary>
    /// Course title. "title" is accepted as an alias for "name" to support both
    /// API callers that use the field name from the response DTO ("title") and the
    /// portal which sends "name".
    /// </summary>
    public string Name { get; init; } = "";
    public string Title { get; init; } = "";
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
