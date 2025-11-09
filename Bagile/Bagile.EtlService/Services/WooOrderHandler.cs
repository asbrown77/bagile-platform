using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Helpers;
using Bagile.EtlService.Mappers;
using Bagile.EtlService.Models;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Services
{
    internal class WooOrderHandler
    {
        private readonly IStudentRepository _studentRepo;
        private readonly IEnrolmentRepository _enrolmentRepo;
        private readonly ICourseScheduleRepository _courseRepo;
        private readonly IFooEventsTicketsClient _fooEventsClient;
        private readonly ILogger _logger;

        public WooOrderHandler(
            IStudentRepository studentRepo,
            IEnrolmentRepository enrolmentRepo,
            ICourseScheduleRepository courseRepo,
            IFooEventsTicketsClient fooEventsClient,
            ILogger logger)
        {
            _studentRepo = studentRepo;
            _enrolmentRepo = enrolmentRepo;
            _courseRepo = courseRepo;
            _fooEventsClient = fooEventsClient;
            _logger = logger;
        }

        public async Task HandleAsync(RawOrder rawOrder, long orderId)
        {
            var isInternalTransfer = IsInternalTransferOrder(rawOrder);

            // 1. Fast path: external orders with ticket data in the Woo payload
            if (!isInternalTransfer)
            {
                var legacyTickets = WooOrderTicketMapper.MapTickets(rawOrder.Payload).ToList();

                if (legacyTickets.Any())
                {
                    _logger.LogInformation(
                        "Processing Woo order {OrderId} using Woo metadata (no FooEvents API).",
                        orderId);

                    await ProcessLegacyTicketsAsync(legacyTickets, rawOrder, orderId);
                    return;
                }
            }

            // 2. Slow / accurate path: FooEvents API
            var externalOrderId = ExtractExternalOrderId(rawOrder.Payload);

            var apiTickets = await _fooEventsClient.FetchTicketsForOrderAsync(
                externalOrderId,
                CancellationToken.None);

            if (!apiTickets.Any())
            {
                _logger.LogWarning(
                    "No FooEvents tickets found for order {OrderId} (internal: {Internal}). Falling back to billing-level enrolment.",
                    orderId,
                    isInternalTransfer);

                await CreateBillingEnrolmentAsync(rawOrder, orderId);
                return;
            }

            _logger.LogInformation(
                "Processing Woo order {OrderId} using FooEvents tickets API ({Count} tickets, internal: {Internal}).",
                orderId,
                apiTickets.Count(),
                isInternalTransfer);

            foreach (var ticket in apiTickets)
            {
                if (IsTicketCancelled(ticket.Status))
                {
                    _logger.LogInformation(
                        "Skipping {Status} ticket {TicketId} for {Email}",
                        ticket.Status,
                        ticket.TicketId,
                        ticket.AttendeeEmail);
                    continue;
                }

                var studentId = await UpsertStudentAsync(ticket);
                var courseScheduleId = await ResolveCourseScheduleAsync(ticket.EventId);

                var transferInfo = TransferParser.ParseDesignation(ticket.Designation);

                if (transferInfo.IsTransfer)
                {
                    // Explicit transfer from designation string (e.g. "Transfer from cancelled PSPO-081025-AB")
                    await HandleIndividualTransferAsync(
                        orderId,
                        studentId,
                        courseScheduleId,
                        ticket,
                        transferInfo);
                }
                else if (isInternalTransfer)
                {
                    // b-agile / bagile order with no explicit designation → try heuristic transfer
                    var courseSchedule = await _courseRepo.GetBySourceProductIdAsync(ticket.EventId);
                    var sku = courseSchedule?.Sku;
                    var courseFamily = ExtractCourseFamilyFromSku(sku);

                    var handled = await TryHandleHeuristicTransferAsync(
                        orderId,
                        studentId,
                        courseScheduleId,
                        courseFamily);

                    if (!handled)
                    {
                        await CreateStandardEnrolmentAsync(orderId, studentId, courseScheduleId);
                    }
                }
                else
                {
                    // Normal external order, no transfer signal → plain enrolment
                    await CreateStandardEnrolmentAsync(orderId, studentId, courseScheduleId);
                }
            }
        }

        private async Task<long?> ResolveCourseScheduleAsync(long? productId)
        {
            if (!productId.HasValue)
                return null;

            // We expect course_schedules to already exist for FooEvents tickets
            var schedule = await _courseRepo.GetBySourceProductIdAsync(productId.Value);
            return schedule?.Id;
        }

        private bool IsInternalTransferOrder(RawOrder rawOrder)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawOrder.Payload);
                var root = doc.RootElement;

                if (!root.TryGetProperty("billing", out var billing))
                    return false;

                var company = billing.TryGetProperty("company", out var coProp)
                    ? coProp.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(company))
                    return false;

                // Treat our own bookings as candidates for transfers
                return company.Equals("b-agile", StringComparison.OrdinalIgnoreCase)
                       || company.Equals("bagile", StringComparison.OrdinalIgnoreCase)
                       || company.Equals("b agile", StringComparison.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private async Task ProcessLegacyTicketsAsync(
            IEnumerable<WooOrderTicketMapper.TicketDto> tickets,
            RawOrder rawOrder,
            long orderId)
        {
            foreach (var ticket in tickets)
            {
                var studentId = await UpsertStudentFromTicketAsync(ticket);
                var courseScheduleId = await ResolveCourseScheduleFromTicketAsync(
                    rawOrder.Payload,
                    ticket.ProductId);

                var enrolment = new Enrolment
                {
                    StudentId = studentId,
                    OrderId = orderId,
                    CourseScheduleId = courseScheduleId,
                    Status = "active"
                };

                // Idempotent for standard enrolments
                await _enrolmentRepo.UpsertAsync(enrolment);
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

        private async Task<long> UpsertStudentAsync(FooEventTicketDto ticket)
        {
            // We only have a full name from FooEvents, so split it crudely
            var fullName = ticket.AttendeeName ?? string.Empty;
            var parts = fullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var firstName = parts.FirstOrDefault();
            var lastName = parts.Length > 1
                ? string.Join(" ", parts.Skip(1))
                : null;

            var student = new Student
            {
                Email = ticket.AttendeeEmail.ToLowerInvariant(),
                FirstName = firstName,
                LastName = lastName,
                Company = null // FooEvents JSON does not give us company
            };

            return await _studentRepo.UpsertAsync(student);
        }

        private async Task<CourseSchedule?> GetCourseScheduleAsync(long? productId)
        {
            if (!productId.HasValue)
                return null;

            return await _courseRepo.GetBySourceProductIdAsync(productId.Value);
        }

        private async Task HandleIndividualTransferAsync(
            long orderId,
            long studentId,
            long? newCourseScheduleId,
            FooEventTicketDto ticket,
            TransferInfo transferInfo)
        {
            _logger.LogInformation(
                "Processing individual transfer for {Email}: original SKU {OriginalSku}, reason {Reason}, refund eligible: {Refund}",
                ticket.AttendeeEmail,
                transferInfo.OriginalSku,
                transferInfo.Reason,
                transferInfo.RefundEligible);

            // Find original enrolment (same order, same student, original SKU)
            var originalEnrolment = await _enrolmentRepo.FindByOrderStudentAndSkuAsync(
                orderId,
                studentId,
                transferInfo.OriginalSku);

            if (originalEnrolment != null)
            {
                // Create new enrolment on the SAME order
                var newEnrolment = new Enrolment
                {
                    StudentId = studentId,
                    OrderId = orderId,
                    CourseScheduleId = newCourseScheduleId,
                    Status = "active",
                    TransferredFromEnrolmentId = originalEnrolment.Id,
                    OriginalSku = transferInfo.OriginalSku,
                    TransferReason = transferInfo.Reason.ToString().ToLower(),
                    TransferNotes = ticket.Designation,
                    RefundEligible = transferInfo.RefundEligible
                };

                var newId = await _enrolmentRepo.InsertAsync(newEnrolment);

                // Mark original as transferred
                await _enrolmentRepo.UpdateStatusAsync(
                    originalEnrolment.Id,
                    "transferred",
                    transferredToEnrolmentId: newId);

                _logger.LogInformation(
                    "Transferred enrolment {OldId} → {NewId} for student {StudentId}",
                    originalEnrolment.Id,
                    newId,
                    studentId);
            }
            else
            {
                _logger.LogWarning(
                    "Transfer ticket but no original enrolment found for " +
                    "student {StudentId}, SKU {Sku}",
                    studentId,
                    transferInfo.OriginalSku);

                // Create as new (might be data issue)
                await CreateStandardEnrolmentAsync(orderId, studentId, newCourseScheduleId);
            }
        }

        private async Task CreateStandardEnrolmentAsync(
            long orderId,
            long studentId,
            long? courseScheduleId)
        {
            var enrolment = new Enrolment
            {
                StudentId = studentId,
                OrderId = orderId,
                CourseScheduleId = courseScheduleId,
                Status = "active"
            };

            await _enrolmentRepo.UpsertAsync(enrolment);
        }

        private string ExtractExternalOrderId(string payload)
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
            {
                return idProp.ToString();
            }

            throw new InvalidOperationException("Cannot extract order ID from payload");
        }

        private async Task<long?> ResolveCourseScheduleFromTicketAsync(
            string payload,
            long? productId)
        {
            if (!productId.HasValue)
                return null;

            var existingId = await _courseRepo.GetIdBySourceProductAsync(productId.Value);
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
                courseData.Currency);
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

            // NEW: infer base date from the order itself
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

            if (root.TryGetProperty("line_items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var pid = TryGetProductIdFromLineItem(item);

                    if (pid == productId || pid == 0)
                    {
                        name ??= item.TryGetProperty("name", out var n) ? n.GetString() : null;

                        if (!price.HasValue && item.TryGetProperty("price", out var p))
                        {
                            price = p.GetDecimal();
                        }

                        if (item.TryGetProperty("sku", out var s))
                        {
                            var skuVal = s.GetString();
                            if (!string.IsNullOrWhiteSpace(skuVal))
                            {
                                sku ??= skuVal;
                            }
                        }
                    }
                }
            }

            if (name != null)
            {
                // pass orderDate into the parser so we infer the right year
                startDate = TryParseStartDateFromName(name, orderDate);
            }

            // Fallback SKU if Woo did not provide one
            if (string.IsNullOrWhiteSpace(sku) && name != null && startDate.HasValue)
            {
                var code = ExtractCourseCodeFromName(name);
                sku = $"{code}-{startDate.Value:ddMMyy}";
            }

            return (name, sku, startDate, endDate, price, currency);
        }


        private static long? TryGetProductIdFromLineItem(JsonElement item)
        {
            if (!item.TryGetProperty("product_id", out var pidProp))
                return null;

            return pidProp.TryGetInt64(out var pidVal) ? pidVal : null;
        }

        private static DateTime? TryParseStartDateFromName(string name, DateTime? baseDate)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var cleaned = name.Replace("™", "").Trim();

            const string marker = " - ";
            var idx = cleaned.LastIndexOf(marker, StringComparison.Ordinal);
            var segment = idx >= 0 ? cleaned[(idx + marker.Length)..].Trim() : cleaned;

            var tokens = segment
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
                return null;

            // Day: first token, possibly "8-10"
            var dayToken = tokens[0];
            if (dayToken.Contains('-'))
                dayToken = dayToken.Split('-')[0];

            if (!int.TryParse(dayToken, out var day) || day <= 0 || day > 31)
                return null;

            // Month: first token that starts with letters
            string? monthText = null;
            for (var i = 0; i < tokens.Length; i++)
            {
                var t = tokens[i];
                var letters = new string(t.TakeWhile(char.IsLetter).ToArray());
                if (!string.IsNullOrEmpty(letters))
                {
                    monthText = letters;
                    break;
                }
            }

            if (string.IsNullOrEmpty(monthText))
                return null;

            if (!DateTime.TryParseExact(
                    monthText,
                    new[] { "MMM", "MMMM" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var monthDate))
            {
                return null;
            }

            var month = monthDate.Month;

            // Year: last purely numeric token (2 or 4 digits)
            int? year = null;
            for (var i = tokens.Length - 1; i >= 0; i--)
            {
                var t = tokens[i];
                if (t.All(char.IsDigit) && (t.Length == 2 || t.Length == 4))
                {
                    if (int.TryParse(t, out var y))
                    {
                        if (t.Length == 2)
                            y += 2000;

                        year = y;
                        break;
                    }
                }
            }

            // If no year in the name, infer from order date, not "now"
            var reference = baseDate ?? DateTime.UtcNow;

            if (!year.HasValue)
            {
                // simple rule:
                //  - if course month >= order month → same year
                //  - if course month < order month → next year (e.g. Jan after a Nov order)
                year = month < reference.Month ? reference.Year + 1 : reference.Year;
            }

            return new DateTime(year.Value, month, day);
        }

        private static string ExtractCourseCodeFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "COURSE";

            var beforeDash = name.Split('-')[0];
            var words = beforeDash
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var chars = words
                .Where(w => char.IsLetter(w[0]) && char.IsUpper(w[0]))
                .Select(w => w[0])
                .ToArray();

            if (chars.Length >= 2)
                return new string(chars);

            return "COURSE";
        }

        private async Task CreateBillingEnrolmentAsync(RawOrder rawOrder, long orderId)
        {
            _logger.LogWarning(
                "No tickets found for order {OrderId}, creating billing-level enrolment",
                orderId);

            // Extract billing info from order JSON
            using var doc = JsonDocument.Parse(rawOrder.Payload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("billing", out var billing))
            {
                _logger.LogWarning("No billing info for order {OrderId}", orderId);
                return;
            }

            var email = billing.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("No billing email for order {OrderId}", orderId);
                return;
            }

            var firstName = billing.TryGetProperty("first_name", out var fnProp)
                ? fnProp.GetString()
                : null;
            var lastName = billing.TryGetProperty("last_name", out var lnProp)
                ? lnProp.GetString()
                : null;
            var company = billing.TryGetProperty("company", out var coProp)
                ? coProp.GetString()
                : null;

            var student = new Student
            {
                Email = email.ToLowerInvariant(),
                FirstName = firstName,
                LastName = lastName,
                Company = company
            };

            var studentId = await _studentRepo.UpsertAsync(student);
            await CreateStandardEnrolmentAsync(orderId, studentId, null);
        }

        private bool IsTicketCancelled(string status)
        {
            return status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                   status.Equals("Refunded", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ExtractCourseFamilyFromSku(string? sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            var parts = sku.Split('-', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : null;
        }

        private async Task<bool> TryHandleHeuristicTransferAsync(
            long orderId,
            long studentId,
            long? newCourseScheduleId,
            string? courseFamily)
        {
            if (newCourseScheduleId == null || string.IsNullOrWhiteSpace(courseFamily))
                return false;

            var source = await _enrolmentRepo.FindHeuristicTransferSourceAsync(
                studentId,
                courseFamily);

            if (source == null)
                return false;

            _logger.LogInformation(
                "Heuristic transfer: student {StudentId}, family {Family}, from enrolment {SourceId} → new schedule {NewScheduleId}",
                studentId,
                courseFamily,
                source.Id,
                newCourseScheduleId);

            var newEnrolment = new Enrolment
            {
                StudentId = studentId,
                OrderId = orderId,
                CourseScheduleId = newCourseScheduleId,
                Status = "active",
                TransferredFromEnrolmentId = source.Id,
                OriginalSku = source.OriginalSku ?? null,
                TransferReason = "attendee_requested", // heuristic default
                TransferNotes = "Inferred transfer from b-agile order",
                RefundEligible = null
            };

            var newId = await _enrolmentRepo.InsertAsync(newEnrolment);

            await _enrolmentRepo.UpdateStatusAsync(
                source.Id,
                "transferred",
                transferredToEnrolmentId: newId);

            return true;
        }
    }
}
