using Bagile.Application.Trainers.Commands;
using Bagile.Application.Trainers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/trainers")]
public class TrainersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TrainersController(IMediator mediator) => _mediator = mediator;

    /// <summary>List all active trainers.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTrainersQuery(), ct);
        return Ok(result);
    }

    /// <summary>Add a new trainer.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] TrainerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "name is required" });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "email is required" });

        var command = new CreateTrainerCommand
        {
            Name  = request.Name,
            Email = request.Email,
            Phone = request.Phone,
        };

        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { }, result);
    }

    /// <summary>Update a trainer's details.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] TrainerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "name is required" });

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "email is required" });

        var command = new UpdateTrainerCommand
        {
            Id    = id,
            Name  = request.Name,
            Email = request.Email,
            Phone = request.Phone,
        };

        var result = await _mediator.Send(command, ct);
        if (result is null)
            return NotFound(new { error = $"Trainer {id} not found" });

        return Ok(result);
    }

    /// <summary>Deactivate (soft-delete) a trainer.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _mediator.Send(new DeleteTrainerCommand(id), ct);
        if (!deleted)
            return NotFound(new { error = $"Trainer {id} not found" });

        return NoContent();
    }
}

// ── Request model ────────────────────────────────────────────

public record TrainerRequest
{
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
}
