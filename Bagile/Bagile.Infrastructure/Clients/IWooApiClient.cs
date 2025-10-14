using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bagile.Infrastructure.Models;

namespace Bagile.Infrastructure.Clients
{
    public interface IWooApiClient
    {
        Task<IReadOnlyList<string>> FetchOrdersAsync(DateTime? modifiedSince, CancellationToken ct);
        Task<IReadOnlyList<string>> FetchOrdersAsync(int page, int perPage, DateTime? modifiedSince, CancellationToken ct);
        Task<IReadOnlyList<WooProduct>> FetchProductsAsync(CancellationToken ct = default);
    }


}
