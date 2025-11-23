using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;

namespace Bagile.EtlService.Services
{
    public class XeroInvoiceService(
        IOrderRepository orderRepo,
        ILogger<XeroInvoiceService> logger)
        : IProcessor<CanonicalXeroInvoiceDto>
    {
        public async Task ProcessAsync(CanonicalXeroInvoiceDto dto, CancellationToken token)
        {
            var order = new Order
            {
                RawOrderId = dto.RawOrderId,
                ExternalId = dto.ExternalId,
                Reference = dto.Reference,
                Source = "xero",
                ContactEmail = dto.BillingEmail,
                ContactName = dto.BillingName,
                BillingCompany = dto.BillingCompany,
                TotalQuantity = 1,
                TotalAmount = dto.Total,
                SubTotal = dto.SubTotal,
                TotalTax = dto.TotalTax,
                PaymentTotal = dto.AmountPaid,
                Currency = dto.Currency,
                Status = dto.Status,
                OrderDate = dto.InvoiceDate,
            };

            await orderRepo.UpsertOrderAsync(order);

            logger.LogInformation(
                "Xero invoice imported ExternalId={ExternalId} Total={Total} {Currency} Status={Status}",
                dto.ExternalId,
                dto.Total,
                dto.Currency,
                dto.Status);
        }
    }
}