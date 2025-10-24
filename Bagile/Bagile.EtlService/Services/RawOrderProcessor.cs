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
            var unprocessed = await _rawRepo.GetUnprocessedAsync(100);
            if (!unprocessed.Any())
            {
                _logger.LogInformation("No unprocessed raw orders found.");
                return;
            }

            foreach (var rawOrder in unprocessed)
            {
                await ProcessSingleOrderAsync(rawOrder, token);
            }
        }

        private async Task ProcessSingleOrderAsync(RawOrder rawOrder, CancellationToken token)
        {
            try
            {
                var order = OrderMapper.MapFromRaw(rawOrder);
                if (order == null)
                {
                    _logger.LogWarning("Skipping unrecognized source '{Source}' for RawOrder {Id}.", rawOrder.Source, rawOrder.Id);
                    await _rawRepo.MarkProcessedAsync(rawOrder.Id);
                    return;
                }

                var orderId = await _orderRepo.UpsertOrderAsync(order, token);

                if (rawOrder.Source.Equals("woo", StringComparison.OrdinalIgnoreCase))
                {
                    await ProcessWooOrderAsync(rawOrder, orderId);
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
                enrolment.CourseScheduleId = await ResolveCourseScheduleIdAsync(enrolment.CourseScheduleId);
                await _enrolmentRepo.UpsertAsync(enrolment);
            }
        }

        private async Task<long?> ResolveCourseScheduleIdAsync(long? productId)
        {
            if (!productId.HasValue)
                return null;

            return await _courseRepo.GetIdBySourceProductAsync("WooCommerce", productId.Value);
        }
    }
}
