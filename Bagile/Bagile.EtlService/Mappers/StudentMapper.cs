using System.Text.Json;
using Bagile.Domain.Entities;

namespace Bagile.EtlService.Mappers
{
    public static class StudentMapper
    {
        public static IEnumerable<Student> MapFromWooOrder(string payload)
        {
            var attendees = new List<Student>();

            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("meta_data", out var metaData))
                return attendees;

            foreach (var meta in metaData.EnumerateArray())
            {
                if (!meta.TryGetProperty("key", out var keyProp) ||
                    keyProp.GetString() != "WooCommerceEventsOrderTickets")
                    continue;

                if (!meta.TryGetProperty("value", out var tickets))
                    continue;

                foreach (var courseEntry in tickets.EnumerateObject())
                {
                    foreach (var ticketEntry in courseEntry.Value.EnumerateObject())
                    {
                        var t = ticketEntry.Value;

                        string first = TryGetString(t, "WooCommerceEventsAttendeeName");
                        string last = TryGetString(t, "WooCommerceEventsAttendeeLastName");
                        string email = TryGetString(t, "WooCommerceEventsAttendeeEmail");
                        string company = TryGetString(t, "WooCommerceEventsAttendeeCompany");

                        // 🧹 Skip invalid or blank entries
                        if (string.IsNullOrWhiteSpace(email))
                            continue;

                        attendees.Add(new Student
                        {
                            FirstName = first,
                            LastName = last,
                            Email = email.ToLowerInvariant(),
                            Company = company
                        });
                    }
                }
            }

            return attendees;
        }

        private static string TryGetString(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var value) &&
                value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? "";

            return "";
        }
    }
}
