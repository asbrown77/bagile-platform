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
        private readonly ILogger<RawOrderProcessor> _logger;

        public RawOrderProcessor(
            IOrderRepository orderRepo,
            IRawOrderRepository rawRepo,
            IStudentRepository studentRepo,
            IEnrolmentRepository enrolmentRepo,
            ILogger<RawOrderProcessor> logger)
        {
            _orderRepo = orderRepo;
            _rawRepo = rawRepo;
            _studentRepo = studentRepo;
            _enrolmentRepo = enrolmentRepo;
            _logger = logger;
        }

        public async Task ProcessPendingAsync(CancellationToken token)
        {
            while (true)
            {
                var unprocessed = await _rawRepo.GetUnprocessedAsync(100);
                if (!unprocessed.Any())
                    break; // nothing left to process

                foreach (var record in unprocessed)
                {
                    try
                    {
                        var order = OrderMapper.MapFromRaw(record.Source, record.Id, record.Payload);
                        if (order == null)
                            continue;

                        // FIXED: capture the new order ID from the repository
                        var orderId = await _orderRepo.UpsertOrderAsync(order, token);

                        // --- Map students + enrolments for WooCommerce orders
                        if (record.Source.Equals("woo", StringComparison.OrdinalIgnoreCase))
                        {
                            var students = StudentMapper.MapFromWooOrder(record.Payload);

                            foreach (var student in students)
                            {
                                var studentId = await _studentRepo.UpsertAsync(student);

                                var enrolments = EnrolmentMapper.MapFromWooOrder(record.Payload, orderId);
                                foreach (var enrol in enrolments)
                                {
                                    await _enrolmentRepo.UpsertAsync(studentId, orderId, enrol.CourseScheduleProductId);
                                }
                            }
                        }

                        await _rawRepo.MarkProcessedAsync(record.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing record {Id}", record.Id);
                        await _rawRepo.MarkFailedAsync(record.Id, ex.Message);
                    }
                }
            }
        }
    }
}
