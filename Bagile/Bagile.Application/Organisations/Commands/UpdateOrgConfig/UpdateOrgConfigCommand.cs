using Bagile.Application.Organisations.DTOs;
using MediatR;

namespace Bagile.Application.Organisations.Commands.UpdateOrgConfig;

public record UpdateOrgConfigCommand : IRequest<OrgConfigDto?>
{
    public long Id { get; init; }
    public List<string> Aliases { get; init; } = new();
    public string? PrimaryDomain { get; init; }
}
