using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Trainers.Commands;

public record DeleteTrainerCommand(int Id) : IRequest<bool>;

public class DeleteTrainerCommandHandler : IRequestHandler<DeleteTrainerCommand, bool>
{
    private readonly ITrainerRepository _repo;

    public DeleteTrainerCommandHandler(ITrainerRepository repo) => _repo = repo;

    public async Task<bool> Handle(DeleteTrainerCommand request, CancellationToken ct)
        => await _repo.DeactivateAsync(request.Id, ct);
}
