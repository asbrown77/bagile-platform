using System.Text.Json;
using Bagile.Domain.Entities;

namespace Bagile.EtlService.Mappers
{
    public static class EnrolmentMapper
    {
        public static IEnumerable<Enrolment> MapFromWooOrder(string payload, long orderId)
        {
            var result = new List<Enrolment>();

            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("meta_data", out var metaData))
                return result;

            foreach (var meta in metaData.EnumerateArray())
            {
                if (!meta.TryGetProperty("key", out var keyProp) ||
                    keyProp.GetString() != "WooCommerceEventsOrderTickets")
                    continue;

                if (!meta.TryGetProperty("value", out var tickets))
                    continue;

                foreach (var courseEntry in tickets.EnumerateObject()) // "1"
                {
                    foreach (var ticketEntry in courseEntry.Value.EnumerateObject()) // "1","2","3","4"
                    {
                        var ticket = ticketEntry.Value;

                        long? productId = null;
                        if (ticket.TryGetProperty("WooCommerceEventsProductID", out var pidProp) &&
                            pidProp.ValueKind == JsonValueKind.String &&
                            long.TryParse(pidProp.GetString(), out var parsedId))
                        {
                            productId = parsedId;
                        }

                        result.Add(new Enrolment
                        {
                            OrderId = orderId,
                            CourseScheduleProductId = productId,
                            CourseScheduleId = null,
                            StudentId = 0
                        });
                    }
                }
            }

            return result;
        }
    }
}