using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Trainers.Queries;

public record GetTrainersQuery : IRequest<IEnumerable<TrainerDto>>;

public class GetTrainersQueryHandler : IRequestHandler<GetTrainersQuery, IEnumerable<TrainerDto>>
{
    private readonly ITrainerRepository _repo;

    public GetTrainersQueryHandler(ITrainerRepository repo) => _repo = repo;

    public async Task<IEnumerable<TrainerDto>> Handle(GetTrainersQuery request, CancellationToken ct)
    {
        var trainers = await _repo.GetAllActiveAsync(ct);
        return trainers.Select(t => new TrainerDto
        {
            Id       = t.Id,
            Name     = t.Name,
            Email    = t.Email,
            Phone    = t.Phone,
            IsActive = t.IsActive,
        });
    }
}
