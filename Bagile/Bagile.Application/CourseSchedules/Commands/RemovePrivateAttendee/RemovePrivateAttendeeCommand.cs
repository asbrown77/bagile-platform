using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.RemovePrivateAttendee;

/// <summary>
/// Cancels a specific enrolment on a private course.
/// Uses enrolmentId (not studentId) to unambiguously target one enrolment
/// even when a student has attended multiple sessions.
/// </summary>
public record RemovePrivateAttendeeCommand(long CourseScheduleId, long EnrolmentId)
    : IRequest<bool>;
