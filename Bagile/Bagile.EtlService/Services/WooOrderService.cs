using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Bagile.EtlService.Helpers;

namespace Bagile.EtlService.Services
{
    public class WooOrderService : IProcessor<CanonicalWooOrderDto>
    {
        private readonly IStudentRepository _studentRepo;
        private readonly IEnrolmentRepository _enrolmentRepo;
        private readonly ICourseScheduleRepository _courseRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly ILogger<WooOrderService> _logger;

        public WooOrderService(
            IStudentRepository studentRepo,
            IEnrolmentRepository enrolmentRepo,
            ICourseScheduleRepository courseRepo,
            IOrderRepository orderRepo,
            ILogger<WooOrderService> logger)
        {
            _studentRepo = studentRepo;
            _enrolmentRepo = enrolmentRepo;
            _courseRepo = courseRepo;
            _orderRepo = orderRepo;
            _logger = logger;
        }

        public async Task ProcessAsync(CanonicalWooOrderDto dto, CancellationToken token)
        {
            // CREATE ORDER
            if (dto.OrderId == 0)
            {
                var order = new Order
                {
                    RawOrderId = dto.RawOrderId,
                    ExternalId = dto.ExternalId,
                    Source = "woo",
                    Type = "public",
                    Reference = dto.ExternalId,

                    BillingCompany = dto.BillingCompany,
                    ContactName = dto.BillingName,
                    ContactEmail = dto.BillingEmail,

                    TotalQuantity = dto.TotalQuantity,
                    SubTotal = dto.SubTotal,
                    TotalTax = dto.TotalTax,
                    TotalAmount = dto.Total,

                    PaymentTotal = dto.PaymentTotal,
                    RefundTotal = dto.RefundTotal,
                    NetTotal = dto.PaymentTotal - dto.RefundTotal,

                    Status = dto.Status,
                    LifecycleStatus = MapLifecycleStatus(dto.Status, dto.PaymentTotal, dto.RefundTotal),
                    Currency = dto.Currency,
                    OrderDate = dto.DateCreated ?? DateTime.UtcNow
                };

                dto.OrderId = await _orderRepo.UpsertOrderAsync(order);
            }

            // Create primary student from first ticket or billing info
            var primaryStudentId = await CreateStudentFromTicketOrBillingAsync(
                dto.Tickets.FirstOrDefault(), dto);

            // TRANSFER HANDLING
            // Only attempt transfer if the primary student has an active enrolment
            // on a DIFFERENT course schedule with the same course type.
            // Same course = new booking, not a transfer.
            bool shouldTryTransfer = false;

            if (dto.Tickets.Count > 0)
            {
                var prefix = ExtractCoursePrefix(dto.Tickets[0].Sku);

                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    var previous = await _enrolmentRepo.FindHeuristicTransferSourceAsync(primaryStudentId, prefix);

                    if (previous != null)
                    {
                        // Only transfer if the old enrolment is on a different course schedule
                        var newScheduleId = await ResolveCourseScheduleAsync(dto.Tickets[0]);
                        if (newScheduleId.HasValue && previous.CourseScheduleId != newScheduleId.Value)
                        {
                            shouldTryTransfer = true;
                        }
                    }
                }
            }

            if (shouldTryTransfer)
            {
                var handled = await TryHandleInternalTransferAsync(dto, primaryStudentId, token);
                if (handled)
                {
                    _logger.LogInformation("Internal transfer handled for order {OrderId}", dto.OrderId);
                    return;
                }
            }

            bool isCancelled = dto.Status == "cancelled"
                               || (dto.RefundTotal > 0 && dto.RefundTotal >= dto.PaymentTotal);

            if (isCancelled)
            {
                await HandleFullCancellationAsync(dto.OrderId);
            }

            // BILLING ONLY
            if (dto.Tickets.Count == 0)
            {
                await CreateBillingOnlyEnrolmentAsync(dto, primaryStudentId, token);
                return;
            }

            // NORMAL ENROLMENTS — one student + enrolment per ticket
            foreach (var ticket in dto.Tickets)
            {
                var studentId = await CreateStudentFromTicketOrBillingAsync(ticket, dto);

                var scheduleId = await ResolveCourseScheduleAsync(ticket);
                if (!scheduleId.HasValue)
                    continue;

                var enrol = new Enrolment
                {
                    StudentId = studentId,
                    OrderId = dto.OrderId,
                    CourseScheduleId = scheduleId.Value,
                    IsCancelled = isCancelled,
                    Status = isCancelled ? "cancelled" : "active"
                };

                await _enrolmentRepo.UpsertAsync(enrol);
            }
        }

        // --------------------------------------------------------------

        private async Task<bool> TryHandleInternalTransferAsync(
            CanonicalWooOrderDto dto,
            long studentId,
            CancellationToken token)
        {
            if (dto.Tickets.Count == 0)
                return false;

            bool moved = false;

            foreach (var t in dto.Tickets)
            {
                var scheduleId = await ResolveCourseScheduleAsync(t);
                if (!scheduleId.HasValue)
                    continue;

                var schedule = new CourseSchedule { Id = scheduleId.Value, Sku = t.Sku };

                var prefix = ExtractCoursePrefix(t.Sku);
                if (string.IsNullOrWhiteSpace(prefix))
                    continue;

                var oldEnrol = await _enrolmentRepo.FindHeuristicTransferSourceAsync(studentId, prefix);
                if (oldEnrol == null)
                    continue;

                // Only transfer if moving to a DIFFERENT course schedule
                if (oldEnrol.CourseScheduleId == scheduleId.Value)
                    continue;

                // check if the new enrolment already exists
                var existing = await _enrolmentRepo.FindAsync(studentId, dto.OrderId, schedule.Id);
                if (existing != null)
                {
                    _logger.LogWarning("Transfer already exists. Skipping duplicate. Student {StudentId}, Order {OrderId}, Schedule {ScheduleId}",
                        studentId, dto.OrderId, schedule.Id);

                    await _enrolmentRepo.MarkTransferredAsync(oldEnrol.Id, existing.Id);
                    moved = true;
                    continue;
                }

                var newEnrol = new Enrolment
                {
                    StudentId = studentId,
                    OrderId = dto.OrderId,
                    CourseScheduleId = schedule.Id,
                    Status = "active",
                    TransferredFromEnrolmentId = oldEnrol.Id,
                    OriginalSku = oldEnrol.OriginalSku ?? schedule.Sku,
                    TransferReason = "CourseTransfer",
                    RefundEligible = true
                };

                var newId = await _enrolmentRepo.InsertAsync(newEnrol);
                await _enrolmentRepo.MarkTransferredAsync(oldEnrol.Id, newId);
                moved = true;
            }

            return moved;
        }

        private string ExtractCoursePrefix(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return "";
            return WooCourseParsingHelper.ExtractCourseCodeFromSku(sku);
        }

        private async Task HandleFullCancellationAsync(long orderId)
        {
            var enrols = await _enrolmentRepo.GetByOrderIdAsync(orderId);

            foreach (var e in enrols)
                await _enrolmentRepo.CancelEnrolmentAsync(e.Id);

            _logger.LogInformation("Cancelled {Count} enrolments for order {OrderId}", enrols.Count(), orderId);
        }

        private async Task CreateBillingOnlyEnrolmentAsync(
            CanonicalWooOrderDto dto,
            long studentId,
            CancellationToken token)
        {
            using var doc = JsonDocument.Parse(dto.RawPayload);
            if (!doc.RootElement.TryGetProperty("line_items", out var items))
                return;

            foreach (var item in items.EnumerateArray())
            {
                long? productId = null;

                if (item.TryGetProperty("product_id", out var pidProp) &&
                    pidProp.TryGetInt64(out var pidVal))
                {
                    productId = pidVal;
                }

                var scheduleId = await ResolveCourseScheduleAsync(new CanonicalTicketDto
                {
                    ProductId = productId,
                    Sku = item.TryGetProperty("sku", out var s) ? s.GetString() : null,
                    RawOrderPayload = doc.RootElement,
                    RawLineItem = item
                });

                if (!scheduleId.HasValue)
                    continue;

                var enrol = new Enrolment
                {
                    StudentId = studentId,
                    OrderId = dto.OrderId,
                    CourseScheduleId = scheduleId.Value,
                    Status = "active"
                };

                await _enrolmentRepo.UpsertAsync(enrol);
            }
        }

        private async Task<long?> ResolveCourseScheduleAsync(CanonicalTicketDto ticket)
        {
            // 1. Try by ProductId (good Woo or FooEvents repaired)
            if (ticket.ProductId.HasValue && ticket.ProductId.Value > 0)
            {
                var schedule = await _courseRepo.GetBySourceProductIdAsync(ticket.ProductId.Value);
                if (schedule != null)
                    return schedule.Id;
            }

            // 2. Try by SKU
            if (!string.IsNullOrWhiteSpace(ticket.Sku))
            {
                var scheduleId = await _courseRepo.GetIdBySkuAsync(ticket.Sku);
                if (scheduleId.HasValue)
                    return scheduleId.Value;
            }

            // 3. Nothing found. Must create a new schedule from Woo metadata
            var fallback = ExtractCourseDetailsFromWoo(ticket.RawOrderPayload, ticket.ProductId ?? 0);

            var newId = await _courseRepo.UpsertFromWooPayloadAsync(
                ticket.ProductId ?? 0,
                fallback.Name,
                fallback.Sku,
                fallback.StartDate,
                fallback.EndDate,
                fallback.Price,
                fallback.Currency
            );

            return newId;
        }

        private static (string? Name, string? Sku, DateTime? StartDate, DateTime? EndDate, decimal? Price, string? Currency)
    ExtractCourseDetailsFromWoo(JsonElement root, long productId)
        {
            string? name = null;
            string? sku = null;
            decimal? price = null;
            string? currency = root.TryGetProperty("currency", out var c) ? c.GetString() : "GBP";
            DateTime? startDate = null;
            DateTime? endDate = null;

            DateTime? orderDate = null;

            if (root.TryGetProperty("date_created", out var dc) &&
                dc.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(dc.GetString(), out var created))
            {
                orderDate = created;
            }
            else if (root.TryGetProperty("date_paid", out var dp) &&
                     dp.ValueKind == JsonValueKind.String &&
                     DateTime.TryParse(dp.GetString(), out var paid))
            {
                orderDate = paid;
            }

            if (root.TryGetProperty("line_items", out var items) &&
                items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    long? pid = WooCourseParsingHelper.TryGetProductIdFromLineItem(item);

                    if (pid == productId || pid == null || productId == 0)
                    {
                        name ??= item.TryGetProperty("name", out var n) ? n.GetString() : null;

                        if (!price.HasValue && item.TryGetProperty("price", out var p))
                        {
                            if (p.ValueKind == JsonValueKind.Number)
                                price = p.GetDecimal();

                            if (p.ValueKind == JsonValueKind.String &&
                                decimal.TryParse(p.GetString(), out var parsedPrice))
                            {
                                price = parsedPrice;
                            }
                        }

                        if (item.TryGetProperty("sku", out var s))
                        {
                            var skuVal = s.GetString();
                            if (!string.IsNullOrWhiteSpace(skuVal))
                                sku ??= skuVal;
                        }
                    }
                }
            }

            if (name != null)
            {
                startDate = WooCourseParsingHelper.TryParseStartDateFromName(name, orderDate);
            }

            if (string.IsNullOrWhiteSpace(sku) && name != null && startDate.HasValue)
            {
                var code = WooCourseParsingHelper.ExtractCourseCodeFromName(name);
                sku = $"{code}-{startDate.Value:ddMMyy}";
            }

            return (name, sku, startDate, endDate, price, currency);
        }


        private async Task<long> CreateStudentFromTicketOrBillingAsync(
            CanonicalTicketDto? ticket, CanonicalWooOrderDto dto)
        {
            Student student;

            if (ticket != null && !string.IsNullOrWhiteSpace(ticket.Email))
            {
                student = new Student
                {
                    Email = ticket.Email.ToLowerInvariant(),
                    FirstName = ticket.FirstName,
                    LastName = ticket.LastName,
                    Company = string.IsNullOrWhiteSpace(ticket.Company)
                        ? dto.BillingCompany
                        : ticket.Company
                };
            }
            else
            {
                student = new Student
                {
                    Email = dto.BillingEmail?.ToLowerInvariant() ?? string.Empty,
                    FirstName = ExtractFirst(dto.BillingName),
                    LastName = ExtractLast(dto.BillingName),
                    Company = dto.BillingCompany
                };
            }

            return await _studentRepo.UpsertAsync(student);
        }

        private string ExtractFirst(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";
            return name.Split(' ').FirstOrDefault() ?? "";
        }

        private string ExtractLast(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";
            var parts = name.Split(' ');
            return parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";
        }

        private string MapLifecycleStatus(string status, decimal paymentTotal, decimal refundTotal)
        {
            if (status == "cancelled") 
                return "cancelled";

            if (refundTotal >= paymentTotal && paymentTotal > 0)
                return "fully_refunded";

            if (refundTotal > 0)
                return "partially_refunded";

            return status == "completed" ? "completed" : "pending";
        }
    }
}
