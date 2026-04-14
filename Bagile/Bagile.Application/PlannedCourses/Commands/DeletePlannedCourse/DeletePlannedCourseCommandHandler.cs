using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.DeletePlannedCourse;

public class DeletePlannedCourseCommandHandler
    : IRequestHandler<DeletePlannedCourseCommand, DeletePlannedCourseResult>
{
    private readonly IPlannedCourseRepository _repo;

    public DeletePlannedCourseCommandHandler(IPlannedCourseRepository repo)
    {
        _repo = repo;
    }

    public async Task<DeletePlannedCourseResult> Handle(
        DeletePlannedCourseCommand request,
        CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(request.Id);
        if (existing == null)
            return new DeletePlannedCourseResult(Found: false, HasPublications: false);

        if (await _repo.HasPublicationsAsync(request.Id))
            return new DeletePlannedCourseResult(Found: true, HasPublications: true);

        await _repo.DeleteAsync(request.Id);
        return new DeletePlannedCourseResult(Found: true, HasPublications: false);
    }
}
