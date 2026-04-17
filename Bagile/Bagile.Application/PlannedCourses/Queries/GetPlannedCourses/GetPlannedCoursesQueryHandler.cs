using Bagile.Application.Common.Interfaces;
using Bagile.Application.PlannedCourses.DTOs;
using MediatR;

namespace Bagile.Application.PlannedCourses.Queries.GetPlannedCourses;

public class GetPlannedCoursesQueryHandler
    : IRequestHandler<GetPlannedCoursesQuery, IEnumerable<PlannedCourseDto>>
{
    private readonly IPlannedCourseQueries _queries;

    public GetPlannedCoursesQueryHandler(IPlannedCourseQueries queries)
    {
        _queries = queries;
    }

    public async Task<IEnumerable<PlannedCourseDto>> Handle(
        GetPlannedCoursesQuery request,
        CancellationToken ct)
    {
        return await _queries.GetAllAsync(ct);
    }
}
