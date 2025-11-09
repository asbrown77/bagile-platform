using MediatR;
using Bagile.Application.Organisations.DTOs;

namespace Bagile.Application.Organisations.Queries.GetOrganisationCourseHistory;

public record GetOrganisationCourseHistoryQuery(string OrganisationName)
    : IRequest<IEnumerable<OrganisationCourseHistoryDto>>;