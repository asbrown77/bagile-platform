using MediatR;
using Bagile.Application.PlannedCourses.DTOs;

namespace Bagile.Application.CourseSchedules.Commands.PublishScrumOrg;

public record PublishScrumOrgForScheduleCommand(long CourseScheduleId) : IRequest<ScrumOrgPublishResultDto>;
