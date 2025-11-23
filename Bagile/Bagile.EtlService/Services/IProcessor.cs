using Bagile.EtlService.Models;

namespace Bagile.EtlService.Services;

public interface IProcessor<TDto>
{
    Task ProcessAsync(TDto dto, CancellationToken token);
}
