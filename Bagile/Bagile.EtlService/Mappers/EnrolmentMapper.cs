using System.Linq;
using System.Text.Json;
using Bagile.Domain.Entities;

namespace Bagile.EtlService.Mappers
{
    public static class EnrolmentMapper
    {
        private const string MetaDataProperty = "meta_data";
        private const string TicketsKey = "WooCommerceEventsOrderTickets";
        private const string ProductIdKey = "WooCommerceEventsProductID";

        public static IEnumerable<Enrolment> MapFromWooOrder(string payload, long orderId, long studentId)
        {
            using var doc = JsonDocument.Parse(payload);

            if (!TryGetTickets(doc.RootElement, out var tickets))
                return Enumerable.Empty<Enrolment>();

            var enrolments = ExtractEnrolments(tickets, orderId, studentId);
            return RemoveDuplicates(enrolments);
        }

        private static bool TryGetTickets(JsonElement root, out JsonElement tickets)
        {
            tickets = default;

            if (!root.TryGetProperty(MetaDataProperty, out var metaData))
                return false;

            var ticketsMeta = FindMetaByKey(metaData, TicketsKey);
            if (ticketsMeta.ValueKind == JsonValueKind.Undefined)
                return false;

            return ticketsMeta.TryGetProperty("value", out tickets);
        }

        private static JsonElement FindMetaByKey(JsonElement metaData, string key)
        {
            foreach (var meta in metaData.EnumerateArray())
            {
                if (!meta.TryGetProperty("key", out var keyProp))
                    continue;

                if (keyProp.GetString() == key)
                    return meta;
            }

            return default;
        }

        private static IEnumerable<Enrolment> ExtractEnrolments(JsonElement tickets, long orderId, long studentId)
        {
            // tickets: { "1": { "1": { ... }, "2": { ... } }, "2": {...} }
            var result = new List<Enrolment>();

            foreach (var courseEntry in tickets.EnumerateObject())
            {
                var attendees = courseEntry.Value;
                result.AddRange(ProcessAttendees(attendees, orderId, studentId));
            }

            return result;
        }

        private static IEnumerable<Enrolment> ProcessAttendees(JsonElement attendees, long orderId, long studentId)
        {
            foreach (var attendeeEntry in attendees.EnumerateObject())
            {
                var ticket = attendeeEntry.Value;
                yield return CreateEnrolment(ticket, orderId, studentId);
            }
        }

        private static Enrolment CreateEnrolment(JsonElement ticket, long orderId, long studentId)
        {
            var courseScheduleId = GetCourseScheduleId(ticket);

            return new Enrolment
            {
                StudentId = studentId,
                OrderId = orderId,
                CourseScheduleId = courseScheduleId
            };
        }

        private static long? GetCourseScheduleId(JsonElement ticket)
        {
            if (!ticket.TryGetProperty(ProductIdKey, out var pidProp))
                return null;

            if (pidProp.ValueKind == JsonValueKind.Number && pidProp.TryGetInt64(out var id))
                return id;

            if (pidProp.ValueKind == JsonValueKind.String && long.TryParse(pidProp.GetString(), out var parsed))
                return parsed;

            return null;
        }

        private static IEnumerable<Enrolment> RemoveDuplicates(IEnumerable<Enrolment> enrolments)
        {
            return enrolments
                .GroupBy(e => new { e.StudentId, e.CourseScheduleId })
                .Select(g => g.First())
                .ToList();
        }
    }
}
