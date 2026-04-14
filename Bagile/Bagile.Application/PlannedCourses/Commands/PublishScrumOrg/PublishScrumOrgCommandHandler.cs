using Bagile.Application.Common.Interfaces;
using Bagile.Application.PlannedCourses.Commands.PublishEcommerce;
using Bagile.Application.PlannedCourses.DTOs;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.PublishScrumOrg;

public class PublishScrumOrgCommandHandler
    : IRequestHandler<PublishScrumOrgCommand, ScrumOrgPublishResultDto>
{
    private readonly IPlannedCourseRepository _courseRepo;
    private readonly ICoursePublicationRepository _pubRepo;
    private readonly ITrainerRepository _trainerRepo;
    private readonly IScrumOrgPublishService _scrumOrgPublish;
    private readonly IWooCommercePublishService _wooPublish;

    public PublishScrumOrgCommandHandler(
        IPlannedCourseRepository courseRepo,
        ICoursePublicationRepository pubRepo,
        ITrainerRepository trainerRepo,
        IScrumOrgPublishService scrumOrgPublish,
        IWooCommercePublishService wooPublish)
    {
        _courseRepo = courseRepo;
        _pubRepo = pubRepo;
        _trainerRepo = trainerRepo;
        _scrumOrgPublish = scrumOrgPublish;
        _wooPublish = wooPublish;
    }

    public async Task<ScrumOrgPublishResultDto> Handle(
        PublishScrumOrgCommand request,
        CancellationToken ct)
    {
        var course = await _courseRepo.GetByIdAsync(request.PlannedCourseId)
            ?? throw new KeyNotFoundException($"Planned course {request.PlannedCourseId} not found");

        if (course.Status == "cancelled")
            throw new InvalidOperationException("Cannot publish a cancelled course");

        // Check ecommerce is published first (Scrum.org needs the product URL)
        var ecommercePub = await _pubRepo.GetByPlannedCourseAndGatewayAsync(
            request.PlannedCourseId, "ecommerce", ct)
            ?? throw new InvalidOperationException("Must publish to E-commerce first");

        // Check scrumorg not already published
        var existingScrumOrg = await _pubRepo.GetByPlannedCourseAndGatewayAsync(
            request.PlannedCourseId, "scrumorg", ct);

        if (existingScrumOrg != null)
            throw new ConflictException("Scrum.org gateway already published for this course");

        var trainer = await _trainerRepo.GetByIdAsync(course.TrainerId, ct)
            ?? throw new KeyNotFoundException($"Trainer {course.TrainerId} not found");

        var result = await _scrumOrgPublish.CreateListingAsync(new ScrumOrgPublishRequest
        {
            CourseType = course.CourseType,
            StartDate = course.StartDate,
            EndDate = course.EndDate,
            TrainerName = trainer.Name,
            RegistrationUrl = ecommercePub.ExternalUrl ?? ""
        }, ct);

        if (result == null)
            throw new InvalidOperationException("Failed to create Scrum.org listing. Check logs for details.");

        // Update the WooCommerce product with the Scrum.org listing URL
        if (ecommercePub.WoocommerceProductId.HasValue)
        {
            await _wooPublish.UpdateProductMetaAsync(
                ecommercePub.WoocommerceProductId.Value,
                new Dictionary<string, string>
                {
                    ["scrumorg_listing_url"] = result.ListingUrl
                },
                ct);
        }

        await _pubRepo.InsertAsync(new CoursePublication
        {
            PlannedCourseId = request.PlannedCourseId,
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
}
