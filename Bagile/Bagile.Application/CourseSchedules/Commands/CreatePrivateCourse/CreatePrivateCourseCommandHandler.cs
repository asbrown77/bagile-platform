using Bagile.Application.Common.Interfaces;
using Bagile.Application.CourseSchedules.DTOs;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.CreatePrivateCourse;

public class CreatePrivateCourseCommandHandler
    : IRequestHandler<CreatePrivateCourseCommand, CourseScheduleDetailDto>
{
    private readonly ICourseScheduleRepository _courseRepo;
    private readonly ICourseScheduleQueries _queries;

    public CreatePrivateCourseCommandHandler(
        ICourseScheduleRepository courseRepo,
        ICourseScheduleQueries queries)
    {
        _courseRepo = courseRepo;
        _queries = queries;
    }

    public async Task<CourseScheduleDetailDto> Handle(
        CreatePrivateCourseCommand request,
        CancellationToken ct)
    {
        var sku = $"{request.CourseCode}-PRIV-{request.StartDate:ddMMyy}";

        if (await _courseRepo.ExistsBySkuAsync(sku))
        {
            var suffix = DateTime.UtcNow.ToString("HHmm");
            sku = $"{request.CourseCode}-PRIV-{request.StartDate:ddMMyy}-{suffix}";
        }

        // Accept either "name" or "title" — the response DTO uses "title" so some
        // callers naturally send that field name back. Title is the fallback.
        var resolvedName = !string.IsNullOrWhiteSpace(request.Name)
            ? request.Name
            : request.Title;

        var schedule = new CourseSchedule
        {
            Name = resolvedName,
            Sku = sku,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            FormatType = request.FormatType,
            TrainerName = request.TrainerName,
            Capacity = request.Capacity,
            Price = request.Price,
            IsPublic = false,
            Status = "confirmed",
            SourceSystem = "portal",
            ClientOrganisationId = request.ClientOrganisationId,
            Notes = request.Notes,
            CreatedBy = "portal",
            InvoiceReference = request.InvoiceReference,
            MeetingUrl = request.MeetingUrl,
            MeetingId = request.MeetingId,
            MeetingPasscode = request.MeetingPasscode,
            VenueAddress = request.VenueAddress
        };

        var id = await _courseRepo.InsertPrivateCourseAsync(schedule);

        var result = await _queries.GetCourseScheduleByIdAsync(id, ct);
        return result!;
    }
}
