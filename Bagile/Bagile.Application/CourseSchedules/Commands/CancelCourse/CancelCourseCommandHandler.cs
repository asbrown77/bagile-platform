using Bagile.Application.Common.Interfaces;
using Bagile.Application.CourseSchedules.DTOs;
using Bagile.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.CourseSchedules.Commands.CancelCourse;

public class CancelCourseCommandHandler
    : IRequestHandler<CancelCourseCommand, CourseScheduleDetailDto?>
{
    private readonly ICourseScheduleRepository _repository;
    private readonly ICourseScheduleQueries _queries;
    private readonly ILogger<CancelCourseCommandHandler> _logger;

    public CancelCourseCommandHandler(
        ICourseScheduleRepository repository,
        ICourseScheduleQueries queries,
        ILogger<CancelCourseCommandHandler> logger)
    {
        _repository = repository;
        _queries = queries;
        _logger = logger;
    }

    public async Task<CourseScheduleDetailDto?> Handle(
        CancelCourseCommand request,
        CancellationToken ct)
    {
        var existing = await _queries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct);
        if (existing == null)
            return null;

        // Idempotent — already cancelled is a no-op
        if (existing.Status is "sold_out" or "cancelled")
        {
            _logger.LogInformation("Course {Id} already cancelled, returning current state", request.CourseScheduleId);
            return existing;
        }

        _logger.LogInformation(
            "Cancelling course schedule {Id} ({Code}). Reason: {Reason}",
            request.CourseScheduleId, existing.CourseCode, request.Reason);

        await _repository.UpdateStatusAsync(request.CourseScheduleId, "cancelled");

        return await _queries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct);
    }
}
