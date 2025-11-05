using System.Text.Json;
using Bagile.Domain.Entities;

namespace Bagile.EtlService.Mappers
{
    public static class StudentMapper
    {
        private const string MetaDataProperty = "meta_data";
        private const string TicketsKey = "WooCommerceEventsOrderTickets";

        private const string AttendeeFirstNameKey = "WooCommerceEventsAttendeeName";
        private const string AttendeeLastNameKey = "WooCommerceEventsAttendeeLastName";
        private const string AttendeeEmailKey = "WooCommerceEventsAttendeeEmail";
        private const string AttendeeCompanyKey = "WooCommerceEventsAttendeeCompany";

        public static IEnumerable<Student> MapFromWooOrder(string payload)
        {
            using var doc = JsonDocument.Parse(payload);

            if (!TryGetTicketsElement(doc.RootElement, out var tickets))
                return Enumerable.Empty<Student>();

            return ExtractStudentsFromTickets(tickets);
        }

        private static bool TryGetTicketsElement(JsonElement root, out JsonElement tickets)
        {
            tickets = default;

            if (!root.TryGetProperty(MetaDataProperty, out var metaData))
                return false;

            // Find the first meta entry with key == WooCommerceEventsOrderTickets
            foreach (var meta in metaData.EnumerateArray())
            {
                if (!meta.TryGetProperty("key", out var keyProp))
                    continue;

                if (!string.Equals(keyProp.GetString(), TicketsKey, StringComparison.Ordinal))
                    continue;

                // Found the right meta, now get its "value"
                if (!meta.TryGetProperty("value", out tickets))
                    return false;

                return true;
            }

            return false;
        }

        private static IEnumerable<Student> ExtractStudentsFromTickets(JsonElement tickets)
        {
            var attendees = new List<Student>();

            // tickets: courseEntry → ticketEntry → ticket object
            foreach (var courseEntry in tickets.EnumerateObject())
            {
                var courseTickets = courseEntry.Value;

                foreach (var ticketEntry in courseTickets.EnumerateObject())
                {
                    var ticket = ticketEntry.Value;

                    var email = GetStringOrEmpty(ticket, AttendeeEmailKey);
                    if (string.IsNullOrWhiteSpace(email))
                        continue;

                    var first = GetStringOrEmpty(ticket, AttendeeFirstNameKey);
                    var last = GetStringOrEmpty(ticket, AttendeeLastNameKey);
                    var company = GetStringOrEmpty(ticket, AttendeeCompanyKey);

                    attendees.Add(new Student
                    {
                        FirstName = first,
                        LastName = last,
                        Email = email.ToLowerInvariant(),
                        Company = company
                    });
                }
            }

            return attendees;
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
