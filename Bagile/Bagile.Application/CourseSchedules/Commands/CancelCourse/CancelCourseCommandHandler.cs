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
        // Null → controller returns 404 (schedule not found)
        var existing = await _queries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct);
        if (existing == null)
        {
            _logger.LogWarning(
                "Cancel request for schedule {Id} — not found.",
                request.CourseScheduleId);
            return null;
        }

        // Idempotent — already cancelled is a no-op
        if (existing.Status is "sold_out" or "cancelled")
        {
            _logger.LogInformation(
                "Course {Id} already cancelled, returning current state",
                request.CourseScheduleId);
            return existing;
        }

        _logger.LogInformation(
            "Cancelling course schedule {Id} ({Code}). Reason: {Reason}",
            request.CourseScheduleId, existing.CourseCode, request.Reason);

        try
        {
            await _repository.UpdateStatusAsync(request.CourseScheduleId, "cancelled");

            // Flip every active attendee to pending_transfer so they appear on the
            // /transfers chase list until rebooked or refunded. Without this, a
            // cancelled course leaves enrolments as 'active' and the attendees fall
            // off the radar entirely.
            var marked = await _repository.MarkActiveEnrolmentsAsPendingTransferAsync(
                request.CourseScheduleId);

            if (marked > 0)
            {
                _logger.LogInformation(
                    "Cancelled course {Id} ({Code}) — {Count} active enrolment(s) marked pending_transfer.",
                    request.CourseScheduleId, existing.CourseCode, marked);
            }
        }
        catch (Exception ex)
        {
            // Wrap DB failures with useful context so the controller's 500 has a
            // meaningful message (previously surfaced as a bare NpgsqlException).
            _logger.LogError(
                ex,
                "Failed to cancel schedule {Id}: UpdateStatusAsync threw.",
                request.CourseScheduleId);
            throw new InvalidOperationException(
                $"Failed to update status for course schedule {request.CourseScheduleId}: {ex.Message}",
                ex);
        }

        // Second read returns the post-cancellation state. If the schedule
        // disappeared between the two reads (unlikely, but possible), fall back
        // to the pre-cancel snapshot with status flipped rather than returning null.
        var updated = await _queries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct);
        if (updated != null)
            return updated;

        _logger.LogWarning(
            "Schedule {Id} disappeared after UpdateStatusAsync; returning pre-cancel snapshot.",
            request.CourseScheduleId);
        return existing with { Status = "cancelled" };
    }
}
