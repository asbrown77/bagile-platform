using Bagile.Application.Common.Interfaces;
using Bagile.Application.PlannedCourses.Commands.PublishEcommerce;
using Bagile.Application.PlannedCourses.DTOs;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.PublishScrumOrg;

public class PublishScrumOrgForScheduleCommandHandler
    : IRequestHandler<PublishScrumOrgForScheduleCommand, ScrumOrgPublishResultDto>
{
    private readonly ICourseScheduleQueries _scheduleQueries;
    private readonly ICoursePublicationRepository _pubRepo;
    private readonly IScrumOrgPublishService _scrumOrgPublish;
    private readonly ITrainerRepository _trainerRepo;

    public PublishScrumOrgForScheduleCommandHandler(
        ICourseScheduleQueries scheduleQueries,
        ICoursePublicationRepository pubRepo,
        IScrumOrgPublishService scrumOrgPublish,
        ITrainerRepository trainerRepo)
    {
        _scheduleQueries = scheduleQueries;
        _pubRepo = pubRepo;
        _scrumOrgPublish = scrumOrgPublish;
        _trainerRepo = trainerRepo;
    }

    public async Task<ScrumOrgPublishResultDto> Handle(
        PublishScrumOrgForScheduleCommand request,
        CancellationToken ct)
    {
        var schedule = await _scheduleQueries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct)
            ?? throw new KeyNotFoundException($"Course schedule {request.CourseScheduleId} not found");

        if (schedule.Status == "cancelled")
            throw new InvalidOperationException("Cannot publish a cancelled course");

        // Check not already published
        var existing = await _pubRepo.GetByScheduleAndGatewayAsync(request.CourseScheduleId, "scrumorg", ct);
        if (existing != null)
            throw new ConflictException("Scrum.org gateway already published for this course schedule");

        // Resolve registration URL: prefer explicit publication record, fall back to source_product_url
        var ecommercePub = await _pubRepo.GetByScheduleAndGatewayAsync(request.CourseScheduleId, "ecommerce", ct);
        var registrationUrl = ecommercePub?.ExternalUrl ?? schedule.SourceProductUrl
            ?? throw new InvalidOperationException("No ecommerce URL found — cannot create Scrum.org listing without a registration URL");

        // Extract course type from SKU (e.g. "APSSD-250526-AB" → "APSSD")
        var courseType = ExtractCourseType(schedule.CourseCode);

        // Resolve trainer credentials: match by name so the publish service uses per-trainer Scrum.org credentials
        var trainers = await _trainerRepo.GetAllActiveAsync(ct);
        var trainer = trainers.FirstOrDefault(t =>
            string.Equals(t.Name, schedule.TrainerName, StringComparison.OrdinalIgnoreCase));

        var result = await _scrumOrgPublish.CreateListingAsync(new ScrumOrgPublishRequest
        {
            CourseType = courseType,
            StartDate = schedule.StartDate ?? DateTime.UtcNow,
            EndDate = schedule.EndDate ?? schedule.StartDate ?? DateTime.UtcNow,
            TrainerName = schedule.TrainerName ?? "",
            RegistrationUrl = registrationUrl,
            TrainerUserId = trainer != null ? $"trainer-{trainer.Id}" : ""
        }, ct);

        if (result == null)
            throw new InvalidOperationException("Failed to create Scrum.org listing. Check logs for details.");

        await _pubRepo.InsertAsync(new CoursePublication
        {
            CourseScheduleId = request.CourseScheduleId,
            Gateway = "scrumorg",
            PublishedAt = DateTime.UtcNow,
            ExternalUrl = result.ListingUrl
        }, ct);

        return new ScrumOrgPublishResultDto
        {
            ListingUrl = result.ListingUrl,
            Status = "created"
        };
    }

    private static readonly HashSet<string> KnownCourseTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PSM", "PSMO", "PSMAI", "PSMA", "PSPO", "PSPOAI", "PSPOA",
        "PSK", "PALE", "PAL", "PSU", "PSFS", "EBM", "PALEBM", "APS", "APSSD",
    };

    private static string ExtractCourseType(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return "";
        var parts = sku.Split('-');
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 6 && parts[i].All(char.IsDigit)) break;
            if (string.Equals(parts[i], "PRIV", StringComparison.OrdinalIgnoreCase)) break;
            if (i + 1 < parts.Length && KnownCourseTypes.Contains(parts[i] + parts[i + 1]))
                return (parts[i] + parts[i + 1]).ToUpperInvariant();
            if (KnownCourseTypes.Contains(parts[i]))
                return parts[i].ToUpperInvariant();
        }
        return parts[0].ToUpperInvariant();
    }
}
