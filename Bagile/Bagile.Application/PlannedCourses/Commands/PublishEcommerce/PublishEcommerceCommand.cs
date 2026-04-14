using Bagile.Application.PlannedCourses.DTOs;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.PublishEcommerce;

public record PublishEcommerceCommand(int PlannedCourseId) : IRequest<EcommercePublishResultDto>;
