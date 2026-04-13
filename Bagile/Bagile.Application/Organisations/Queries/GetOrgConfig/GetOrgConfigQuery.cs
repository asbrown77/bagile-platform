using Bagile.Application.Organisations.DTOs;
using MediatR;

namespace Bagile.Application.Organisations.Queries.GetOrgConfig;

public record GetOrgConfigQuery(string Name) : IRequest<OrgConfigDto?>;
