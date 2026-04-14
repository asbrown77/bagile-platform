using Bagile.Application.PlannedCourses.DTOs;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.PublishScrumOrg;

public record PublishScrumOrgCommand(int PlannedCourseId) : IRequest<ScrumOrgPublishResultDto>;
