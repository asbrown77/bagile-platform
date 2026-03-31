using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.AddPrivateAttendees;

public class AddPrivateAttendeesCommandHandler
    : IRequestHandler<AddPrivateAttendeesCommand, AddPrivateAttendeesResult>
{
    private readonly ICourseScheduleQueries _courseQueries;
    private readonly IStudentRepository _studentRepo;
    private readonly IEnrolmentRepository _enrolmentRepo;

    public AddPrivateAttendeesCommandHandler(
        ICourseScheduleQueries courseQueries,
        IStudentRepository studentRepo,
        IEnrolmentRepository enrolmentRepo)
    {
        _courseQueries = courseQueries;
        _studentRepo = studentRepo;
        _enrolmentRepo = enrolmentRepo;
    }

    public async Task<AddPrivateAttendeesResult> Handle(
        AddPrivateAttendeesCommand request,
        CancellationToken ct)
    {
        var course = await _courseQueries.GetCourseScheduleByIdAsync(
            request.CourseScheduleId, ct);

        if (course == null)
            return new AddPrivateAttendeesResult
            {
                TotalSubmitted = request.Attendees.Count,
                Errors = ["Course schedule not found"]
            };

        int created = 0;
        int alreadyEnrolled = 0;
        var errors = new List<string>();

        foreach (var attendee in request.Attendees)
        {
            if (string.IsNullOrWhiteSpace(attendee.Email))
            {
                errors.Add($"Skipped {attendee.FirstName} {attendee.LastName}: no email");
                continue;
            }

            var student = new Student
            {
                Email = attendee.Email.Trim().ToLowerInvariant(),
                FirstName = attendee.FirstName.Trim(),
                LastName = attendee.LastName.Trim(),
                Company = attendee.Company?.Trim(),
                Country = attendee.Country?.Trim()
            };

            var studentId = await _studentRepo.UpsertAsync(student);

            if (await _enrolmentRepo.ExistsByStudentAndCourseAsync(
                    studentId, request.CourseScheduleId))
            {
                alreadyEnrolled++;
                continue;
            }

            await _enrolmentRepo.InsertWithoutOrderAsync(
                studentId, request.CourseScheduleId, "portal");
            created++;
        }

        return new AddPrivateAttendeesResult
        {
            TotalSubmitted = request.Attendees.Count,
            Created = created,
            AlreadyEnrolled = alreadyEnrolled,
            Errors = errors
        };
    }
}
