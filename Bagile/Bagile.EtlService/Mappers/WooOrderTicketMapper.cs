using System.Text.Json;

namespace Bagile.EtlService.Mappers
{
    public static class WooOrderTicketMapper
    {
        private const string MetaDataProperty = "meta_data";
        private const string TicketsKey = "WooCommerceEventsOrderTickets";
        private const string ProductIdKey = "WooCommerceEventsProductID";

        private const string AttendeeFirstNameKey = "WooCommerceEventsAttendeeName";
        private const string AttendeeLastNameKey = "WooCommerceEventsAttendeeLastName";
        private const string AttendeeEmailKey = "WooCommerceEventsAttendeeEmail";
        private const string AttendeeCompanyKey = "WooCommerceEventsAttendeeCompany";

        public sealed record TicketDto(
            string? FirstName,
            string? LastName,
            string Email,
            string? Company,
            long? ProductId
        );

        public static IEnumerable<TicketDto> MapTickets(string payload)
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (!TryGetTicketsElement(root, out var tickets))
                return Enumerable.Empty<TicketDto>();

            return ExtractTickets(tickets);
        }

        private static bool TryGetTicketsElement(JsonElement root, out JsonElement tickets)
        {
            tickets = default;

            if (!root.TryGetProperty(MetaDataProperty, out var metaData))
                return false;

            foreach (var meta in metaData.EnumerateArray())
            {
                if (!meta.TryGetProperty("key", out var keyProp))
                    continue;

                if (!string.Equals(keyProp.GetString(), TicketsKey, StringComparison.Ordinal))
                    continue;

                if (!meta.TryGetProperty("value", out tickets))
                    return false;

                return true;
            }

            return false;
        }

        private static IEnumerable<TicketDto> ExtractTickets(JsonElement tickets)
        {
            var result = new List<TicketDto>();

            // tickets: { "1": { "1": { ... }, "2": { ... } }, "2": {...} }
            foreach (var courseEntry in tickets.EnumerateObject())
            {
                var attendees = courseEntry.Value;

                foreach (var attendeeEntry in attendees.EnumerateObject())
                {
                    var ticket = attendeeEntry.Value;

                    var email = GetStringOrEmpty(ticket, AttendeeEmailKey);
                    if (string.IsNullOrWhiteSpace(email))
                        continue;

                    var first = GetStringOrEmpty(ticket, AttendeeFirstNameKey);
                    var last = GetStringOrEmpty(ticket, AttendeeLastNameKey);
                    var company = GetStringOrEmpty(ticket, AttendeeCompanyKey);
                    var productId = GetProductId(ticket);

                    result.Add(new TicketDto(
                        FirstName: first,
                        LastName: last,
                        Email: email.Trim(),
                        Company: company,
                        ProductId: productId
                    ));
                }
            }

            return result;
        }

        private static long? GetProductId(JsonElement ticket)
        {
            if (!ticket.TryGetProperty(ProductIdKey, out var pidProp))
                return null;

            if (pidProp.ValueKind == JsonValueKind.Number && pidProp.TryGetInt64(out var id))
                return id;

            if (pidProp.ValueKind == JsonValueKind.String && long.TryParse(pidProp.GetString(), out var parsed))
                return parsed;

            return null;
        }

        private static string GetStringOrEmpty(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
