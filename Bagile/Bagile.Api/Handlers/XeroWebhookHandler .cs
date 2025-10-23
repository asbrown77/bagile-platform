using Bagile.Api.DTO;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Filters;
using Bagile.Infrastructure.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bagile.Api.Handlers;

public class XeroWebhookHandler : IWebhookHandler
{
    private readonly IXeroApiClient _xeroClient;
    public string Source => "xero";

    public XeroWebhookHandler(IXeroApiClient xeroClient)
    {
        _xeroClient = xeroClient;
    }

    public bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger)
    {
        var secret = config.GetValue<string>("Xero:WebhookSecret");
        if (string.IsNullOrEmpty(secret))
        {
            logger.LogWarning("No Xero webhook secret configured");
            return false;
        }

        if (!http.Request.Headers.TryGetValue("x-xero-signature", out var header))
        {
            logger.LogWarning("Missing x-xero-signature header");
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedBase64 = Convert.ToBase64String(hmac.ComputeHash(bodyBytes));
        var valid = string.Equals(header, computedBase64, StringComparison.Ordinal);

        logger.LogInformation("Xero webhook signature validation result: {Valid}", valid);
        return valid;
    }

    public WebhookPayload? PreparePayload(string body, HttpContext http, IConfiguration config, ILogger logger)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);

            if (!doc.RootElement.TryGetProperty("events", out var events) ||
                events.ValueKind != JsonValueKind.Array ||
                events.GetArrayLength() == 0)
            {
                logger.LogInformation("Xero webhook handshake or empty payload, acknowledging.");
                return new WebhookPayload(Source, string.Empty, "handshake", body);
            }

            var evt = events[0];
            var externalId = evt.GetProperty("resourceId").GetString();
            if (string.IsNullOrEmpty(externalId))
            {
                logger.LogWarning("Webhook missing resourceId field. Raw body: {Body}", body);
                return new WebhookPayload(Source, string.Empty, "invalid", body);
            }

            var category = evt.GetProperty("eventCategory").GetString() ?? "UNKNOWN";
            var type = evt.GetProperty("eventType").GetString() ?? "UNKNOWN";
            var eventType = $"{category}.{type}".ToLower();

            XeroInvoice? invoice = null;

            try
            {
                invoice = _xeroClient.GetInvoiceByIdAsync(externalId).GetAwaiter().GetResult();
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("404"))
            {
                logger.LogWarning("Invoice {Id} not found (404). Ignoring.", externalId);
            }

            if (invoice == null)
            {
                logger.LogInformation("No valid invoice returned for {Id}. Skipping insert.", externalId);
                return new WebhookPayload(Source, externalId, eventType, body);
            }

            if (!XeroInvoiceFilter.ShouldCapture(invoice))
            {
                logger.LogInformation("Filtered out Xero invoice {Id}", invoice.InvoiceID);
                return new WebhookPayload(Source, externalId, eventType, body);
            }

            var json = JsonSerializer.Serialize(invoice);
            logger.LogInformation("Captured Xero invoice {Id}", invoice.InvoiceID);
            return new WebhookPayload(Source, externalId, eventType, json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid Xero payload");
            return null;
        }
    }



}
