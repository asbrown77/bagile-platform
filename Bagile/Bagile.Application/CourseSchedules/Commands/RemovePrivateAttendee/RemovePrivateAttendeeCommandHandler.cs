using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.RemovePrivateAttendee;

public class RemovePrivateAttendeeCommandHandler
    : IRequestHandler<RemovePrivateAttendeeCommand, bool>
{
    private readonly IEnrolmentRepository _enrolments;

    public RemovePrivateAttendeeCommandHandler(IEnrolmentRepository enrolments)
    {
        _enrolments = enrolments;
    }

    public async Task<bool> Handle(RemovePrivateAttendeeCommand request, CancellationToken ct)
    {
        // Validates: enrolment belongs to this course AND course is private.
        // Returns false (404) if the enrolment doesn't exist, is already cancelled,
        // or belongs to a public course.
        return await _enrolments.CancelPrivateEnrolmentAsync(
            request.EnrolmentId,
            request.CourseScheduleId);
    }
}
