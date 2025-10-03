using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bagile.Infrastructure;

namespace Bagile.Api;

public class WooWebhookHandler
{
    private readonly IConfiguration _config;
    private readonly IRawOrderRepository _repo;
    private readonly ILogger _logger;

    public WooWebhookHandler(IConfiguration config, IRawOrderRepository repo, ILogger logger)
    {
        _config = config;
        _repo = repo;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(HttpContext http)
    {
        var bodyBytes = await ReadRequestBodyAsync(http);
        if (bodyBytes.Length == 0)
            return LogAndReturn("Received empty webhook payload", Results.BadRequest("Empty payload"));

        if (IsPayloadTooLarge(bodyBytes, out var tooLargeResult))
            return LogAndReturn($"Payload too large: {bodyBytes.Length} bytes", tooLargeResult);

        if (!IsValidSignature(http, bodyBytes, out var unauthorizedResult))
            return unauthorizedResult;

        var body = Encoding.UTF8.GetString(bodyBytes);
        if (!TryExtractExternalId(body, out var externalId, out var badRequestResult))
            return badRequestResult;

        // Get Woo event type from headers (e.g. "order.created")
        var eventType = http.Request.Headers["X-WC-Webhook-Topic"].FirstOrDefault();

        try
        {
            var id = await _repo.InsertAsync("woo", externalId, body, eventType);
            _logger.LogInformation("Stored raw order externalId={ExternalId}, eventType={EventType}, id={Id}",
                externalId, eventType, id);

            return Results.Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert raw order externalId={ExternalId}, eventType={EventType}",
                externalId, eventType);
            return Results.Problem("Failed to persist payload", statusCode: StatusCodes.Status500InternalServerError);
        }
    }


    private async Task<byte[]> ReadRequestBodyAsync(HttpContext http)
    {
        await using var ms = new MemoryStream();
        await http.Request.Body.CopyToAsync(ms);
        return ms.ToArray();
    }

    private bool IsPayloadTooLarge(byte[] bodyBytes, out IResult result)
    {
        var maxBytes = _config.GetValue<int?>("Webhook:MaxBodySizeBytes") ?? (1 * 1024 * 1024);
        if (bodyBytes.Length > maxBytes)
        {
            result = Results.Problem("Payload too large", statusCode: StatusCodes.Status413PayloadTooLarge);
            return true;
        }
        result = null!;
        return false;
    }

    private bool IsValidSignature(HttpContext http, byte[] bodyBytes, out IResult result)
    {
        var secret = _config.GetValue<string>("WooCommerce:WebhookSecret");
        if (!string.IsNullOrEmpty(secret))
        {
            if (!http.Request.Headers.TryGetValue("X-WC-Webhook-Signature", out var sigHeaders) || string.IsNullOrEmpty(sigHeaders))
            {
                _logger.LogWarning("Missing webhook signature header");
                result = Results.Unauthorized();
                return false;
            }
            var providedSig = sigHeaders.ToString();
            if (!ValidateSignature(bodyBytes, secret, providedSig))
            {
                _logger.LogWarning("Invalid webhook signature");
                result = Results.Unauthorized();
                return false;
            }
        }
        result = null!;
        return true;
    }

    private bool TryExtractExternalId(string body, out string externalId, out IResult result)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            externalId = doc.RootElement.GetProperty("id").GetRawText().Trim('"');
            result = null!;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid WooCommerce payload: missing or malformed 'id'");
            externalId = "";
            result = Results.BadRequest("Invalid WooCommerce payload");
            return false;
        }
    }

    private IResult LogAndReturn(string message, IResult result)
    {
        _logger.LogWarning(message);
        return result;
    }

    private static bool ValidateSignature(byte[] bodyBytes, string secret, string providedSig)
    {
        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computed = hmac.ComputeHash(bodyBytes);
            var computedBase64 = Convert.ToBase64String(computed);

            var prov = Convert.FromBase64String(providedSig);
            var comp = Convert.FromBase64String(computedBase64);

            return prov.Length == comp.Length && CryptographicOperations.FixedTimeEquals(prov, comp);
        }
        catch
        {
            return false;
        }
    }
}