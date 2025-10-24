using System.Text.Json;
using Bagile.Domain.Entities;

namespace Bagile.EtlService.Mappers
{
    public static class EnrolmentMapper
    {
        public static IEnumerable<Enrolment> MapFromWooOrder(string payload, long orderId, long studentId)
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

                foreach (var courseEntry in tickets.EnumerateObject())
                {
                    foreach (var ticketEntry in courseEntry.Value.EnumerateObject())
                    {
                        var ticket = ticketEntry.Value;
                        long? courseScheduleId = null;

                        if (ticket.TryGetProperty("WooCommerceEventsProductID", out var pidProp) &&
                            long.TryParse(pidProp.GetString(), out var parsedId))
                        {
                            // this can later be resolved to an actual FK in ETL or repo layer
                            courseScheduleId = parsedId;
                        }

                        result.Add(new Enrolment
                        {
                            StudentId = studentId,
                            OrderId = orderId,
                            CourseScheduleId = courseScheduleId
                        });
                    }
                }
            }

            return result;
        }
    }
}