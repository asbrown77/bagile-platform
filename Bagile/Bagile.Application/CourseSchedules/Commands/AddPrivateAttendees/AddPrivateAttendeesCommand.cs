using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.AddPrivateAttendees;

public record AddPrivateAttendeesCommand : IRequest<AddPrivateAttendeesResult>
{
    public long CourseScheduleId { get; init; }
    public List<AttendeeInput> Attendees { get; init; } = new();
}

public record AttendeeInput
{
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Company { get; init; }
    public string? Country { get; init; }
}

public record AddPrivateAttendeesResult
{
    public int TotalSubmitted { get; init; }
    public int Created { get; init; }
    public int AlreadyEnrolled { get; init; }
    public List<string> Errors { get; init; } = new();
}
