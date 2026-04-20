using Bagile.Application.PlannedCourses.DTOs;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.PublishScrumOrg;

/// <param name="ExternalUrl">
/// When supplied, skip the Playwright automation and record this pre-created listing URL directly.
/// </param>
public record PublishScrumOrgCommand(int PlannedCourseId, string? ExternalUrl = null) : IRequest<ScrumOrgPublishResultDto>;
