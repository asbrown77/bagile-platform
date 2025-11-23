using Bagile.Domain.Entities;
using Bagile.EtlService.Models;

namespace Bagile.EtlService.Services;

public interface IParser<TDto>
{
    Task<TDto> Parse(RawOrder raw);
}