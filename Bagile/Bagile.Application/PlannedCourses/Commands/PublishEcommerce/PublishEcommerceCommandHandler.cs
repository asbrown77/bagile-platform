using Bagile.Application.Common.Interfaces;
using Bagile.Application.PlannedCourses.DTOs;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.PublishEcommerce;

public class PublishEcommerceCommandHandler
    : IRequestHandler<PublishEcommerceCommand, EcommercePublishResultDto>
{
    private readonly IPlannedCourseRepository _courseRepo;
    private readonly ICoursePublicationRepository _pubRepo;
    private readonly ITrainerRepository _trainerRepo;
    private readonly IWooCommercePublishService _wooPublish;

    public PublishEcommerceCommandHandler(
        IPlannedCourseRepository courseRepo,
        ICoursePublicationRepository pubRepo,
        ITrainerRepository trainerRepo,
        IWooCommercePublishService wooPublish)
    {
        _courseRepo = courseRepo;
        _pubRepo = pubRepo;
        _trainerRepo = trainerRepo;
        _wooPublish = wooPublish;
    }

    public async Task<EcommercePublishResultDto> Handle(
        PublishEcommerceCommand request,
        CancellationToken ct)
    {
        var course = await _courseRepo.GetByIdAsync(request.PlannedCourseId)
            ?? throw new KeyNotFoundException($"Planned course {request.PlannedCourseId} not found");

        if (course.Status == "cancelled")
            throw new InvalidOperationException("Cannot publish a cancelled course");

        var existing = await _pubRepo.GetByPlannedCourseAndGatewayAsync(
            request.PlannedCourseId, "ecommerce", ct);

        if (existing != null)
            throw new ConflictException("E-commerce gateway already published for this course");

        var trainer = await _trainerRepo.GetByIdAsync(course.TrainerId, ct)
            ?? throw new KeyNotFoundException($"Trainer {course.TrainerId} not found");

        var result = await _wooPublish.CreateProductAsync(new WooPublishRequest
        {
            CourseType = course.CourseType,
            StartDate = course.StartDate,
            EndDate = course.EndDate,
            TrainerName = trainer.Name,
            IsVirtual = course.IsVirtual,
            Venue = course.Venue
        }, ct);

        if (result == null)
            throw new InvalidOperationException("Failed to create WooCommerce product. Check logs for details.");

        await _pubRepo.InsertAsync(new CoursePublication
        {
            PlannedCourseId = request.PlannedCourseId,
            Gateway = "ecommerce",
            PublishedAt = DateTime.UtcNow,
            ExternalUrl = result.ProductUrl,
            WoocommerceProductId = (int)result.ProductId
        }, ct);

        return new EcommercePublishResultDto
        {
            ProductId = result.ProductId,
            ProductUrl = result.ProductUrl,
            Status = result.Warnings.Count > 0 ? "created_with_warnings" : "created",
            Warnings = result.Warnings
        };
    }
}

/// <summary>
/// Thrown when the operation conflicts with existing state (e.g., already published).
/// Controller maps this to 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
