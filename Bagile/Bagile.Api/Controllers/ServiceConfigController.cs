using Bagile.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Bagile.Api.Controllers;

/// <summary>
/// Admin endpoint for reading and writing service_config key-value pairs.
/// Protected by the standard API key middleware.
/// </summary>
[ApiController]
[Route("api/admin/service-config")]
public class ServiceConfigController : ControllerBase
{
    private readonly IServiceConfigRepository _repo;

    public ServiceConfigController(IServiceConfigRepository repo) => _repo = repo;

    /// <summary>List all service config keys and values.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var all = await _repo.GetAllAsync(ct);
        return Ok(all);
    }

    /// <summary>Upsert a single service config value.</summary>
    [HttpPut("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert(string key, [FromBody] ServiceConfigUpsertRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new { error = "key is required" });

        if (string.IsNullOrWhiteSpace(request.Value))
            return BadRequest(new { error = "value is required" });

        await _repo.SetAsync(key, request.Value, ct);
        return Ok(new { ok = true, key });
    }
}

public record ServiceConfigUpsertRequest(string Value);
