using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Trainers.Commands;

public record CreateTrainerCommand : IRequest<TrainerDto>
{
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
}

public class CreateTrainerCommandHandler : IRequestHandler<CreateTrainerCommand, TrainerDto>
{
    private readonly ITrainerRepository _repo;

    public CreateTrainerCommandHandler(ITrainerRepository repo) => _repo = repo;

    public async Task<TrainerDto> Handle(CreateTrainerCommand request, CancellationToken ct)
    {
        var trainer = new Trainer
        {
            Name  = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone?.Trim(),
        };

        var created = await _repo.CreateAsync(trainer, ct);
        return new TrainerDto
        {
            Id       = created.Id,
            Name     = created.Name,
            Email    = created.Email,
            Phone    = created.Phone,
            IsActive = created.IsActive,
        };
    }
}
