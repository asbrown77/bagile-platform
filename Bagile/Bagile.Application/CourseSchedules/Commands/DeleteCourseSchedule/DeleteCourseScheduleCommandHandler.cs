using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.CourseSchedules.Commands.DeleteCourseSchedule;

public class DeleteCourseScheduleCommandHandler
    : IRequestHandler<DeleteCourseScheduleCommand, DeleteCourseScheduleResult>
{
    private readonly ICourseScheduleRepository _scheduleRepo;
    private readonly IEnrolmentRepository _enrolmentRepo;
    private readonly ICourseScheduleQueries _queries;
    private readonly ILogger<DeleteCourseScheduleCommandHandler> _logger;

    public DeleteCourseScheduleCommandHandler(
        ICourseScheduleRepository scheduleRepo,
        IEnrolmentRepository enrolmentRepo,
        ICourseScheduleQueries queries,
        ILogger<DeleteCourseScheduleCommandHandler> logger)
    {
        _scheduleRepo = scheduleRepo;
        _enrolmentRepo = enrolmentRepo;
        _queries = queries;
        _logger = logger;
    }

    public async Task<DeleteCourseScheduleResult> Handle(
        DeleteCourseScheduleCommand request,
        CancellationToken ct)
    {
        var existing = await _queries.GetCourseScheduleByIdAsync(request.Id, ct);
        if (existing == null)
            return new DeleteCourseScheduleResult(false, "not_found");

        var activeEnrolments = await _enrolmentRepo.CountActiveByScheduleAsync(request.Id);
        if (activeEnrolments > 0)
        {
            _logger.LogWarning(
                "Refused to delete course schedule {Id}: {Count} active enrolment(s) exist",
                request.Id, activeEnrolments);
            return new DeleteCourseScheduleResult(false, $"has_enrolments:{activeEnrolments}");
        }

        _logger.LogInformation(
            "Hard-deleting course schedule {Id} ({Sku}) — confirmed 0 enrolments",
            request.Id, existing.CourseCode);

        var deleted = await _scheduleRepo.DeleteAsync(request.Id);
        return new DeleteCourseScheduleResult(deleted, deleted ? null : "not_found");
    }
}
