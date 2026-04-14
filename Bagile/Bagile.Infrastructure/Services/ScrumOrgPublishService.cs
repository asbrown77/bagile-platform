using System.Diagnostics;
using System.Text.Json;
using Bagile.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bagile.Infrastructure.Services;

/// <summary>
/// Creates Scrum.org course listings by shelling out to a Playwright Node.js script.
/// The script handles login, finding the template course, copying, and editing.
///
/// In production, this runs on a machine with Node.js and Playwright installed.
/// The Docker API container proxies the request to a worker that has browser access.
/// For now, the script runs locally.
/// </summary>
public class ScrumOrgPublishService : IScrumOrgPublishService
{
    private readonly ILogger<ScrumOrgPublishService> _logger;
    private readonly string _scriptPath;
    private readonly string _username;
    private readonly string _password;

    public ScrumOrgPublishService(IConfiguration config, ILogger<ScrumOrgPublishService> logger)
    {
        _logger = logger;
        _scriptPath = config["ScrumOrg:ScriptPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "scripts", "publish-scrumorg.js");
        _username = config["ScrumOrg:Username"] ?? "";
        _password = config["ScrumOrg:Password"] ?? "";
    }

    public async Task<ScrumOrgPublishResult?> CreateListingAsync(
        ScrumOrgPublishRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating Scrum.org listing for {CourseType} on {StartDate} by {Trainer}",
            request.CourseType, request.StartDate.ToString("yyyy-MM-dd"), request.TrainerName);

        try
        {
            // Build the arguments for the Playwright script
            var args = new Dictionary<string, string>
            {
                ["courseType"] = request.CourseType,
                ["startDate"] = request.StartDate.ToString("yyyy-MM-dd"),
                ["endDate"] = request.EndDate.ToString("yyyy-MM-dd"),
                ["trainerName"] = request.TrainerName,
                ["registrationUrl"] = request.RegistrationUrl,
                ["username"] = _username,
                ["password"] = _password
            };

            var argsJson = JsonSerializer.Serialize(args);

            var psi = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = $"\"{_scriptPath}\" '{argsJson}'",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogError("Failed to start Playwright script process");
                return null;
            }

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Playwright script failed with exit code {Code}: {Stderr}",
                    process.ExitCode, stderr);
                return null;
            }

            // Script outputs JSON with listingUrl
            var result = JsonSerializer.Deserialize<ScriptOutput>(stdout.Trim(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.ListingUrl == null)
            {
                _logger.LogError("Playwright script returned no listing URL. Output: {Output}", stdout);
                return null;
            }

            _logger.LogInformation("Scrum.org listing created: {Url}", result.ListingUrl);

            return new ScrumOrgPublishResult
            {
                ListingUrl = result.ListingUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Scrum.org publish script");
            return null;
        }
    }

    private record ScriptOutput
    {
        public string? ListingUrl { get; init; }
    }
}
