using Bagile.Application.Common.Interfaces;
using Bagile.Application.PlannedCourses.DTOs;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.CreatePlannedCourse;

public class CreatePlannedCourseCommandHandler
    : IRequestHandler<CreatePlannedCourseCommand, PlannedCourseDto>
{
    private readonly IPlannedCourseRepository _repo;
    private readonly IPlannedCourseQueries _queries;

    public CreatePlannedCourseCommandHandler(
        IPlannedCourseRepository repo,
        IPlannedCourseQueries queries)
    {
        _repo = repo;
        _queries = queries;
    }

    public async Task<PlannedCourseDto> Handle(
        CreatePlannedCourseCommand request,
        CancellationToken ct)
    {
        var entity = new PlannedCourse
        {
            CourseType = request.CourseType.ToUpperInvariant(),
            TrainerId = request.TrainerId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsVirtual = request.IsVirtual,
            Venue = request.Venue,
            Notes = request.Notes,
            DecisionDeadline = request.DecisionDeadline,
            IsPrivate = request.IsPrivate,
            Status = "planned"
        };

        var id = await _repo.InsertAsync(entity);
        var result = await _queries.GetByIdAsync(id, ct);
        return result!;
    }
}
