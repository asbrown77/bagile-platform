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
        // Use the invoice reference as the SKU when provided (format: ORG-TYPE-DDMMYY,
        // e.g. "FNC-PSM-270426") — this matches the Xero naming convention and gives
        // courses a meaningful identifier. Fall back to TYPE-PRIV-DDMMYY for legacy callers.
        var baseSku = !string.IsNullOrWhiteSpace(request.InvoiceReference)
            ? request.InvoiceReference.Trim().ToUpperInvariant()
            : $"{request.CourseCode.ToUpperInvariant()}-PRIV-{request.StartDate:ddMMyy}";

        var sku = baseSku;
        if (await _courseRepo.ExistsBySkuAsync(sku))
        {
            var suffix = DateTime.UtcNow.ToString("HHmm");
            sku = $"{baseSku}-{suffix}";
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
