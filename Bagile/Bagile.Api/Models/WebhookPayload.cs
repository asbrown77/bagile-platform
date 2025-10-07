namespace Bagile.Api.DTO;

public record WebhookPayload(
    string Source,
    string ExternalId,
    string EventType,
    string PayloadJson
);