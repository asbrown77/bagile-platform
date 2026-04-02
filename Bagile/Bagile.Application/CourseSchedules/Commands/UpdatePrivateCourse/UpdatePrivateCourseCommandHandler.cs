using Bagile.Application.Common.Interfaces;
using Bagile.Application.CourseSchedules.DTOs;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.UpdatePrivateCourse;

public class UpdatePrivateCourseCommandHandler
    : IRequestHandler<UpdatePrivateCourseCommand, CourseScheduleDetailDto?>
{
    private readonly ICourseScheduleRepository _courseRepo;
    private readonly ICourseScheduleQueries _queries;

    public UpdatePrivateCourseCommandHandler(
        ICourseScheduleRepository courseRepo,
        ICourseScheduleQueries queries)
    {
        _courseRepo = courseRepo;
        _queries = queries;
    }

    public async Task<CourseScheduleDetailDto?> Handle(
        UpdatePrivateCourseCommand request,
        CancellationToken ct)
    {
        var fields = new UpdatePrivateCourseFields(
            Name: request.Name,
            TrainerName: request.TrainerName,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            Capacity: request.Capacity,
            Price: request.Price,
            InvoiceReference: request.InvoiceReference,
            VenueAddress: request.VenueAddress,
            MeetingUrl: request.MeetingUrl,
            MeetingId: request.MeetingId,
            MeetingPasscode: request.MeetingPasscode,
            Notes: request.Notes
        );

        await _courseRepo.UpdatePrivateCourseAsync(request.Id, fields);

        return await _queries.GetCourseScheduleByIdAsync(request.Id, ct);
    }
}
