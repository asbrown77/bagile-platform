using Bagile.Domain.Entities;
using Bagile.EtlService.Models;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Services
{
    public class RawOrderRouter
    {
        private readonly IParser<CanonicalWooOrderDto> _wooParser;
        private readonly IProcessor<CanonicalWooOrderDto> _wooService;

        private readonly IParser<CanonicalXeroInvoiceDto> _xeroParser;
        private readonly IProcessor<CanonicalXeroInvoiceDto> _xeroService;

        private readonly ILogger<RawOrderRouter> _logger;

        public RawOrderRouter(
            IParser<CanonicalWooOrderDto> wooParser,
            IProcessor<CanonicalWooOrderDto> wooService,
            IParser<CanonicalXeroInvoiceDto> xeroParser,
            IProcessor<CanonicalXeroInvoiceDto> xeroService,
            ILogger<RawOrderRouter> logger)
        {
            _wooParser = wooParser;
            _wooService = wooService;
            _xeroParser = xeroParser;
            _xeroService = xeroService;
            _logger = logger;
        }

        public async Task RouteAsync(RawOrder raw, CancellationToken token)
        {
            switch (raw.Source)
            {
                case "WooCommerce":
                    var woocommenceDto = await _wooParser.Parse(raw);
                    await _wooService.ProcessAsync(woocommenceDto, token);
                    break;

                case "woo":
                    var wooDto = await _wooParser.Parse(raw);
                    await _wooService.ProcessAsync(wooDto, token);
                    break;

                case "xero":
                    var xeroDto = await _xeroParser.Parse(raw);
                    await _xeroService.ProcessAsync(xeroDto, token);
                    break;

                default:
                    _logger.LogWarning("Unknown raw order source: {Source}", raw.Source);
                    break;
            }
        }
    }
}