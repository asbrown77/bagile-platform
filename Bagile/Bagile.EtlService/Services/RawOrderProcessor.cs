using System.Text.Json;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Mappers;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Services
{
    public class RawOrderProcessor
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IRawOrderRepository _rawRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IEnrolmentRepository _enrolmentRepo;
        private readonly ICourseScheduleRepository _courseRepo;
        private readonly ILogger<RawOrderProcessor> _logger;

        public RawOrderProcessor(
            IOrderRepository orderRepo,
            IRawOrderRepository rawRepo,
            IStudentRepository studentRepo,
            IEnrolmentRepository enrolmentRepo,
            ICourseScheduleRepository courseRepo,
            ILogger<RawOrderProcessor> logger)
        {
            _orderRepo = orderRepo;
            _rawRepo = rawRepo;
            _studentRepo = studentRepo;
            _enrolmentRepo = enrolmentRepo;
            _courseRepo = courseRepo;
            _logger = logger;
        }

        public async Task ProcessPendingAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var unprocessed = await _rawRepo.GetUnprocessedAsync(100);

                if (!unprocessed.Any())
                {
                    _logger.LogInformation("No unprocessed raw orders found. All done.");
                    break;
                }

                _logger.LogInformation("Processing batch of {Count} raw orders...", unprocessed.Count());

                foreach (var rawOrder in unprocessed)
                {
                    await ProcessSingleOrderAsync(rawOrder, token);
                }

                // small breather between batches
                await Task.Delay(TimeSpan.FromSeconds(2), token);
            }
        }

        private async Task ProcessSingleOrderAsync(RawOrder rawOrder, CancellationToken token)
        {
            try
            {
                var order = OrderMapper.MapFromRaw(rawOrder);
                if (order == null)
                {
                    _logger.LogWarning(
                        "Skipping unrecognized source '{Source}' for RawOrder {Id}.",
                        rawOrder.Source, rawOrder.Id);

                    await _rawRepo.MarkProcessedAsync(rawOrder.Id);
                    return;
                }

                var orderId = await _orderRepo.UpsertOrderAsync(order, token);

                if (rawOrder.Source.Equals("woo", StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessWooOrderAsync(rawOrder, orderId);
                }

                // --- Sanity check: orders without enough enrolments ---
                if (order.TotalQuantity.HasValue)
                {
                    var enrolmentCount = await _enrolmentRepo.CountByOrderIdAsync(orderId);

                    if (enrolmentCount < order.TotalQuantity.Value)
                    {
                        _logger.LogWarning(
                            "Order {OrderId}, {Name}, expected {ExpectedQty} attendees, found only {ActualQty}.",
                            orderId,
                            order.BillingCompany,
                            order.TotalQuantity,
                            enrolmentCount
                        );
                    }
                }

                await _rawRepo.MarkProcessedAsync(rawOrder.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RawOrder {Id}", rawOrder.Id);
                await _rawRepo.MarkFailedAsync(rawOrder.Id, ex.Message);
            }
        }

        private async Task ProcessWooOrderAsync(RawOrder rawOrder, long orderId)
        {
            var students = StudentMapper.MapFromWooOrder(rawOrder.Payload);

            foreach (var student in students)
            {
                var studentId = await _studentRepo.UpsertAsync(student);
                await CreateEnrolmentsForStudentAsync(rawOrder, orderId, studentId);
            }
        }

        private async Task CreateEnrolmentsForStudentAsync(RawOrder rawOrder, long orderId, long studentId)
        {
            var enrolments = EnrolmentMapper.MapFromWooOrder(rawOrder.Payload, orderId, studentId);

            foreach (var enrolment in enrolments)
            {
                // Try to resolve by product ID first
                var resolvedId = await _courseRepo.GetIdBySourceProductAsync("woo", enrolment.CourseScheduleId ?? 0);

                if (!resolvedId.HasValue && enrolment.CourseScheduleId.HasValue)
                {
                    // Extract details from Woo payload (line_items, etc.)
                    var courseData = ExtractCourseDetailsFromWoo(rawOrder.Payload, enrolment.CourseScheduleId.Value);

                    // Auto-create or update missing course schedule
                    resolvedId = await _courseRepo.UpsertFromWooPayloadAsync(
                        enrolment.CourseScheduleId.Value,
                        courseData.Name,
                        courseData.Sku,
                        courseData.StartDate,
                        courseData.EndDate,
                        courseData.Price,
                        courseData.Currency
                    );
                }

                enrolment.CourseScheduleId = resolvedId;
                await _enrolmentRepo.UpsertAsync(enrolment);
            }
        }

        private async Task<long?> ResolveCourseScheduleIdAsync(long? productId)
        {
            if (!productId.HasValue)
                return null;

            return await _courseRepo.GetIdBySourceProductAsync("WooCommerce", productId.Value);
        }

        private static (string? Name, string? Sku, DateTime? StartDate, DateTime? EndDate, decimal? Price, string? Currency)
            ExtractCourseDetailsFromWoo(string payload, long productId)
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            string? name = null;
            string? sku = null;
            decimal? price = null;
            string? currency = root.TryGetProperty("currency", out var c) ? c.GetString() : "GBP";
            DateTime? startDate = null;
            DateTime? endDate = null;

            // Try line_items
            if (root.TryGetProperty("line_items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    // Sometimes product_id is zero, so fallback to meta WooCommerceEventsProductID
                    long? pid = item.TryGetProperty("product_id", out var pidProp) && pidProp.TryGetInt64(out var pidVal)
                        ? pidVal
                        : null;

                    if (pid == productId || pid == 0)
                    {
                        name ??= item.TryGetProperty("name", out var n) ? n.GetString() : null;
                        price ??= item.TryGetProperty("price", out var p) ? p.GetDecimal() : null;
                    }
                }
            }

            // Try to read the schedule from the course name pattern
            if (name != null)
            {
                // Example: "Professional Scrum Master™ - 7-8 Aug 25"
                var parts = name.Split('-');
                if (parts.Length >= 2 && DateTime.TryParse(parts.Last().Trim().Replace("™", ""), out var parsedDate))
                    startDate = parsedDate; // optional: parse both start and end dates from text
            }

            return (name, sku, startDate, endDate, price, currency);
        }
    }
}
