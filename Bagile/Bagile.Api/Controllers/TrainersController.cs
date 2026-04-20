using Bagile.Application.Common.Interfaces;
using Bagile.Application.Trainers.Commands;
using Bagile.Application.Trainers.Queries;
using Bagile.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bagile.Api.Controllers;

[ApiController]
[Route("api/trainers")]
public class TrainersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITrainerRepository _trainerRepo;
    private readonly IPaCredentialService _paCredentials;

    public TrainersController(
        IMediator mediator,
        ITrainerRepository trainerRepo,
        IPaCredentialService paCredentials)
    {
        _mediator = mediator;
        _trainerRepo = trainerRepo;
        _paCredentials = paCredentials;
    }

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

    // ── Scrum.org credentials ────────────────────────────────

    /// <summary>Get a trainer's Scrum.org credential status.</summary>
    [HttpGet("{id:int}/scrumorg-credentials")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScrumOrgCredentials(int id, CancellationToken ct)
    {
        var trainer = await _trainerRepo.GetByIdAsync(id, ct);
        if (trainer is null)
            return NotFound(new { error = $"Trainer {id} not found" });

        var status = await _paCredentials.GetTrainerScrumOrgStatusAsync(id, ct);
        return Ok(status);
    }

    /// <summary>Set a trainer's Scrum.org username and/or password.</summary>
    [HttpPut("{id:int}/scrumorg-credentials")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetScrumOrgCredentials(
        int id,
        [FromBody] ScrumOrgCredentialRequest body,
        CancellationToken ct)
    {
        var trainer = await _trainerRepo.GetByIdAsync(id, ct);
        if (trainer is null)
            return NotFound(new { error = $"Trainer {id} not found" });

        if (string.IsNullOrWhiteSpace(body.Username) && string.IsNullOrWhiteSpace(body.Password))
            return BadRequest(new { error = "Provide at least one of username or password" });

        if (!string.IsNullOrWhiteSpace(body.Username))
            await _paCredentials.SetTrainerScrumOrgCredentialAsync(id, "scrumorg_username", body.Username.Trim(), ct);

        if (!string.IsNullOrWhiteSpace(body.Password))
            await _paCredentials.SetTrainerScrumOrgCredentialAsync(id, "scrumorg_password", body.Password, ct);

        return NoContent();
    }

    /// <summary>Refresh a trainer's Scrum.org session via Playwright login.</summary>
    [HttpPost("{id:int}/scrumorg-credentials/refresh-session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshScrumOrgSession(int id, CancellationToken ct)
    {
        var trainer = await _trainerRepo.GetByIdAsync(id, ct);
        if (trainer is null)
            return NotFound(new { error = $"Trainer {id} not found" });

        var result = await _paCredentials.RefreshTrainerSessionAsync(id, ct);
        return Ok(new { result.Success, result.ErrorMessage });
    }
}

// ── Request models ───────────────────────────────────────────

public record TrainerRequest
{
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
}

public record ScrumOrgCredentialRequest
{
    public string? Username { get; init; }
    public string? Password { get; init; }
}
