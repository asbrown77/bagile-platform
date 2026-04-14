using Bagile.Application.Common.Interfaces;
using Bagile.Application.PlannedCourses.DTOs;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.UpdatePlannedCourse;

public class UpdatePlannedCourseCommandHandler
    : IRequestHandler<UpdatePlannedCourseCommand, PlannedCourseDto?>
{
    private readonly IPlannedCourseRepository _repo;
    private readonly IPlannedCourseQueries _queries;

    public UpdatePlannedCourseCommandHandler(
        IPlannedCourseRepository repo,
        IPlannedCourseQueries queries)
    {
        _repo = repo;
        _queries = queries;
    }

    public async Task<PlannedCourseDto?> Handle(
        UpdatePlannedCourseCommand request,
        CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing == null)
            return null;

        // Merge: only overwrite fields that were explicitly provided
        existing.CourseType = request.CourseType?.ToUpperInvariant() ?? existing.CourseType;
        existing.TrainerId = request.TrainerId ?? existing.TrainerId;
        existing.StartDate = request.StartDate ?? existing.StartDate;
        existing.EndDate = request.EndDate ?? existing.EndDate;
        existing.IsVirtual = request.IsVirtual ?? existing.IsVirtual;
        existing.Venue = request.Venue ?? existing.Venue;
        existing.Notes = request.Notes ?? existing.Notes;
        existing.DecisionDeadline = request.DecisionDeadline ?? existing.DecisionDeadline;
        existing.IsPrivate = request.IsPrivate ?? existing.IsPrivate;

        await _repo.UpdateAsync(request.Id, existing);
        return await _queries.GetByIdAsync(request.Id, ct);
    }
}
