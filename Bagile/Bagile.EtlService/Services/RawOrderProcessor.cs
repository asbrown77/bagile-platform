using System;
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
    public class RawOrderProcessor
    {
        private const string WooSource = "woo";
        private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(2);

        private readonly IOrderRepository _orderRepo;
        private readonly IRawOrderRepository _rawRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IEnrolmentRepository _enrolmentRepo;
        private readonly ICourseScheduleRepository _courseRepo;
        private readonly IFooEventsTicketsClient _fooEventsClient;
        private readonly ILogger<RawOrderProcessor> _logger;

        public RawOrderProcessor(
            IOrderRepository orderRepo,
            IRawOrderRepository rawRepo,
            IStudentRepository studentRepo,
            IEnrolmentRepository enrolmentRepo,
            ICourseScheduleRepository courseRepo,
            IFooEventsTicketsClient fooEventsClient,
            ILogger<RawOrderProcessor> logger)
        {
            _orderRepo = orderRepo;
            _rawRepo = rawRepo;
            _studentRepo = studentRepo;
            _enrolmentRepo = enrolmentRepo;
            _courseRepo = courseRepo;
            _fooEventsClient = fooEventsClient;
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
            var externalOrderId = ExtractExternalOrderId(rawOrder.Payload);

            // 1. Fetch tickets from WordPress API
            var apiTickets = await _fooEventsClient.FetchTicketsForOrderAsync(
                externalOrderId,
                CancellationToken.None);

            if (!apiTickets.Any())
            {
                _logger.LogInformation(
                    "No API tickets for order {OrderId}, falling back to metadata",
                    orderId);
                await ProcessLegacyOrderAsync(rawOrder, orderId);
                return;
            }

            // 2. Process each ticket individually
            foreach (var ticket in apiTickets)
            {
                // Skip cancelled/refunded tickets (old tickets before transfer)
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

                // Check if this ticket is a transfer
                var transferInfo = TransferParser.ParseDesignation(ticket.Designation);

                if (transferInfo.IsTransfer)
                {
                    await HandleIndividualTransferAsync(
                        orderId,
                        studentId,
                        courseScheduleId,
                        ticket,
                        transferInfo);
                }
                else
                {
                    await CreateStandardEnrolmentAsync(orderId, studentId, courseScheduleId);
                }
            }
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

        private async Task<long?> ResolveCourseScheduleAsync(long? productId)
        {
            if (!productId.HasValue)
                return null;

            var schedule = await _courseRepo.GetBySourceProductIdAsync(productId.Value);
            return schedule?.Id;
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

        private async Task ProcessLegacyOrderAsync(RawOrder rawOrder, long orderId)
        {
            // Fallback to existing metadata extraction
            var tickets = WooOrderTicketMapper.MapTickets(rawOrder.Payload);

            if (tickets.Any())
            {
                foreach (var ticket in tickets)
                {
                    var student = new Student
                    {
                        Email = ticket.Email.ToLowerInvariant(),
                        FirstName = ticket.FirstName,
                        LastName = ticket.LastName,
                        Company = ticket.Company
                    };

                    var studentId = await _studentRepo.UpsertAsync(student);
                    var courseScheduleId = await ResolveCourseScheduleFromTicketAsync(ticket.ProductId);
                    await CreateStandardEnrolmentAsync(orderId, studentId, courseScheduleId);
                }
            }
            else
            {
                // Ultimate fallback: billing info
                await CreateBillingEnrolmentAsync(rawOrder, orderId);
            }
        }

        private async Task<long?> ResolveCourseScheduleFromTicketAsync(long? productId)
        {
            if (!productId.HasValue)
                return null;

            return await ResolveCourseScheduleAsync(productId.Value);
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
    }
}
