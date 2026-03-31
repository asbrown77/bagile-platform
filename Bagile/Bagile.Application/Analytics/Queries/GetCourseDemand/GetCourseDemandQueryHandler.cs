using Bagile.Application.Analytics.DTOs;
using Bagile.Application.Common.Interfaces;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetCourseDemand;

public class GetCourseDemandQueryHandler
    : IRequestHandler<GetCourseDemandQuery, CourseDemandResultDto>
{
    private readonly IAnalyticsQueries _queries;

    public GetCourseDemandQueryHandler(IAnalyticsQueries queries)
    {
        _queries = queries;
    }

    public async Task<CourseDemandResultDto> Handle(
        GetCourseDemandQuery request,
        CancellationToken ct)
    {
        var courseTypes = await _queries.GetCourseDemandAsync(request.Months, ct);
        var monthly = await _queries.GetCourseDemandMonthlyAsync(request.Months, ct);

        return new CourseDemandResultDto
        {
            LookbackMonths = request.Months,
            CourseTypes = courseTypes,
            MonthlyTrend = monthly
        };
    }
}
