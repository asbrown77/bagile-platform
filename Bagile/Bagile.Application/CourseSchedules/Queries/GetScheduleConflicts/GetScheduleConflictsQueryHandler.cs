using Bagile.Application.Common.Interfaces;
using MediatR;

namespace Bagile.Application.CourseSchedules.Queries.GetScheduleConflicts;

public class GetScheduleConflictsQueryHandler
    : IRequestHandler<GetScheduleConflictsQuery, IEnumerable<ScheduleConflictDto>>
{
    private readonly ICourseScheduleQueries _queries;

    public GetScheduleConflictsQueryHandler(ICourseScheduleQueries queries)
    {
        _queries = queries;
    }

    public async Task<IEnumerable<ScheduleConflictDto>> Handle(
        GetScheduleConflictsQuery request,
        CancellationToken ct)
    {
        return await _queries.GetScheduleConflictsAsync(
            request.StartDate, request.EndDate, request.TrainerName, ct);
    }
}
