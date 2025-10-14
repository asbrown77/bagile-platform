using Bagile.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.EtlService.Projectors;

public interface IImporter<TSource> where TSource : class
{
    Task ApplyAsync(TSource product, CancellationToken ct = default);
}