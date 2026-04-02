using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Trainers.Commands;

public record UpdateTrainerCommand : IRequest<TrainerDto?>
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
}

public class UpdateTrainerCommandHandler : IRequestHandler<UpdateTrainerCommand, TrainerDto?>
{
    private readonly ITrainerRepository _repo;

    public UpdateTrainerCommandHandler(ITrainerRepository repo) => _repo = repo;

    public async Task<TrainerDto?> Handle(UpdateTrainerCommand request, CancellationToken ct)
    {
        var trainer = new Trainer
        {
            Id    = request.Id,
            Name  = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone?.Trim(),
        };

        var updated = await _repo.UpdateAsync(trainer, ct);
        if (updated is null) return null;

        return new TrainerDto
        {
            Id       = updated.Id,
            Name     = updated.Name,
            Email    = updated.Email,
            Phone    = updated.Phone,
            IsActive = updated.IsActive,
        };
    }
}
