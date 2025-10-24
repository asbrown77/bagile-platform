using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.Domain.Repositories;

public interface IOrderRepository
{
    Task UpsertOrderAsync(
        long rawOrderId,
        string externalId,
        string source,
        string type,
        string? billingCompany,
        string? contactName,
        string? contactEmail,
        decimal totalAmount,
        string? status,
        DateTime? orderDate,
        CancellationToken token);
}


