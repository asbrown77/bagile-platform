using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bagile.Infrastructure.Models;

namespace Bagile.Infrastructure.Clients;

public interface IXeroApiClient
{
    Task<XeroInvoice?> GetInvoiceByIdAsync(string invoiceId);
    Task<IEnumerable<string>> FetchInvoicesAsync(DateTime? modifiedSince = null,
        CancellationToken ct = default);
}
