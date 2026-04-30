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
        // CourseCode drives the SKU when explicitly provided; InvoiceReference is the
        // Xero invoice number and must not overwrite the course's canonical SKU.
        var newSku = !string.IsNullOrWhiteSpace(request.CourseCode)
            ? request.CourseCode.Trim().ToUpperInvariant()
            : null;

        var fields = new UpdatePrivateCourseFields(
            Name: request.Name,
            TrainerName: request.TrainerName,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            Capacity: request.Capacity,
            Price: request.Price,
            ClientOrganisationId: request.ClientOrganisationId,
            InvoiceReference: request.InvoiceReference,
            VenueAddress: request.VenueAddress,
            MeetingUrl: request.MeetingUrl,
            MeetingId: request.MeetingId,
            MeetingPasscode: request.MeetingPasscode,
            Notes: request.Notes,
            Status: request.Status,
            Sku: newSku
        );

        await _courseRepo.UpdatePrivateCourseAsync(request.Id, fields);

        return await _queries.GetCourseScheduleByIdAsync(request.Id, ct);
    }
}
