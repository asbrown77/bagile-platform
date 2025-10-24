using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface IOrderRepository
{
    Task UpsertOrderAsync(Order order, CancellationToken token);
}


