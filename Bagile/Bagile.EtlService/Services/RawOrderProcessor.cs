using System.Linq;
using System.Text.Json;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Mappers;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Services
{
    public class RawOrderProcessor
    {
        private const string WooSource = "woo";
        private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(2);

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

                await Task.Delay(BatchDelay, token);
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

                if (rawOrder.Source.Equals(WooSource, StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessWooOrderAsync(rawOrder, orderId);
                }

                await ValidateEnrolmentConsistencyAsync(order, orderId);

                await _rawRepo.MarkProcessedAsync(rawOrder.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RawOrder {Id}", rawOrder.Id);
                await _rawRepo.MarkFailedAsync(rawOrder.Id, ex.Message);
            }
        }

        private async Task ValidateEnrolmentConsistencyAsync(Order order, long orderId)
        {
            if (!order.TotalQuantity.HasValue)
                return;

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

        private async Task ProcessWooOrderAsync(RawOrder rawOrder, long orderId)
        {
            var tickets = WooOrderTicketMapper.MapTickets(rawOrder.Payload);

            foreach (var ticket in tickets)
            {
                var studentId = await UpsertStudentFromTicketAsync(ticket);
                var courseScheduleId = await ResolveCourseScheduleFromTicketAsync(rawOrder.Payload, ticket.ProductId);
                await UpsertEnrolmentAsync(orderId, studentId, courseScheduleId);
            }
        }

        private async Task<long> UpsertStudentFromTicketAsync(WooOrderTicketMapper.TicketDto ticket)
        {
            var student = new Student
            {
                FirstName = ticket.FirstName,
                LastName = ticket.LastName,
                Email = ticket.Email.ToLowerInvariant(),
                Company = ticket.Company
            };

            return await _studentRepo.UpsertAsync(student);
        }

        private async Task<long?> ResolveCourseScheduleFromTicketAsync(string payload, long? productId)
        {
            if (!productId.HasValue)
                return null;

            var existingId = await _courseRepo.GetIdBySourceProductAsync(WooSource, productId.Value);
            if (existingId.HasValue)
                return existingId;

            var courseData = ExtractCourseDetailsFromWoo(payload, productId.Value);

            return await _courseRepo.UpsertFromWooPayloadAsync(
                productId.Value,
                courseData.Name,
                courseData.Sku,
                courseData.StartDate,
                courseData.EndDate,
                courseData.Price,
                courseData.Currency
            );
        }

        private async Task UpsertEnrolmentAsync(long orderId, long studentId, long? courseScheduleId)
        {
            var enrolment = new Enrolment
            {
                StudentId = studentId,
                OrderId = orderId,
                CourseScheduleId = courseScheduleId
            };

            await _enrolmentRepo.UpsertAsync(enrolment);
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

            if (root.TryGetProperty("line_items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var pid = TryGetProductIdFromLineItem(item);

                    if (pid == productId || pid == 0)
                    {
                        name ??= item.TryGetProperty("name", out var n) ? n.GetString() : null;
                        price ??= item.TryGetProperty("price", out var p) ? p.GetDecimal() : null;
                        sku ??= item.TryGetProperty("sku", out var s) ? s.GetString() : null;
                    }
                }
            }

            if (name != null)
            {
                startDate = TryParseStartDateFromName(name);
            }

            return (name, sku, startDate, endDate, price, currency);
        }

        private static long? TryGetProductIdFromLineItem(JsonElement item)
        {
            if (!item.TryGetProperty("product_id", out var pidProp))
                return null;

            return pidProp.TryGetInt64(out var pidVal) ? pidVal : null;
        }

        private static DateTime? TryParseStartDateFromName(string name)
        {
            var parts = name.Split('-');
            var lastPart = parts.LastOrDefault()?.Trim().Replace("™", "");

            if (string.IsNullOrWhiteSpace(lastPart))
                return null;

            return DateTime.TryParse(lastPart, out var parsedDate)
                ? parsedDate
                : null;
        }
    }
}
