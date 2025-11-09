using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Organisations.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Organisations.Queries.GetOrganisationCourseHistory;

public class GetOrganisationCourseHistoryQueryHandler
    : IRequestHandler<GetOrganisationCourseHistoryQuery, IEnumerable<OrganisationCourseHistoryDto>>
{
    private readonly IOrganisationQueries _queries;
    private readonly ILogger<GetOrganisationCourseHistoryQueryHandler> _logger;

    public GetOrganisationCourseHistoryQueryHandler(
        IOrganisationQueries queries,
        ILogger<GetOrganisationCourseHistoryQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<IEnumerable<OrganisationCourseHistoryDto>> Handle(
        GetOrganisationCourseHistoryQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching course history for organisation: Name={Name}",
            request.OrganisationName);

        return await _queries.GetOrganisationCourseHistoryAsync(request.OrganisationName, ct);
    }
}