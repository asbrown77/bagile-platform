using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bagile.EtlService.Collectors;
using Bagile.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Bagile.UnitTests
{
    [TestFixture]
    public class XeroCollectorTests
    {
        [Test]
        public async Task CollectAsync_ShouldReturnInvoices_FromApiClient()
        {
            var mockApi = new Mock<IXeroApiClient>();
            mockApi.Setup(x => x.FetchInvoicesAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>
                {
                    """{"InvoiceID":"1001","Type":"ACCREC","Status":"PAID","Reference":"dummy"}""",
                    """{"InvoiceID":"2002","Type":"ACCREC","Status":"AUTHORISED","Reference":"dummy"}""",
                    """{"InvoiceID":"3003","Type":"ACCPAY","Status":"PAID","Reference":"dummy"}""" // should be filtered out
                });

            var collector = new XeroCollector(mockApi.Object, NullLogger<XeroCollector>.Instance);

            var result = await collector.CollectAsync(null);

            result.Should().HaveCount(2);
            result.Should().Contain(r => r.Contains("1001"));
            result.Should().Contain(r => r.Contains("2002"));

            mockApi.Verify(x => x.FetchInvoicesAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void SourceName_ShouldBeXero()
        {
            var collector = new XeroCollector(Mock.Of<IXeroApiClient>(), NullLogger<XeroCollector>.Instance);
            collector.SourceName.Should().Be("xero");
        }

        [Test]
        public async Task CollectAsync_ShouldIgnore_InvalidTypeOrStatus()
        {
            var mockApi = new Mock<IXeroApiClient>();
            mockApi.Setup(x => x.FetchInvoicesAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>
                {
                    """{"InvoiceID":"4001","Type":"ACCPAY","Status":"PAID"}""",
                    """{"InvoiceID":"4002","Type":"ACCREC","Status":"DRAFT"}""",
                    """{"InvoiceID":"4003","Type":"ACCREC","Status":"DELETED"}"""
                });

            var collector = new XeroCollector(mockApi.Object, NullLogger<XeroCollector>.Instance);

            var result = await collector.CollectAsync(null);

            result.Should().BeEmpty("none of the invoices have valid type or status");
        }

        [Test]
        public async Task CollectAsync_ShouldIgnore_PublicInvoices_StartingWithHash()
        {
            var mockApi = new Mock<IXeroApiClient>();
            mockApi.Setup(x => x.FetchInvoicesAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>
                {
                    """{"InvoiceID":"5001","Type":"ACCREC","Status":"PAID","Reference":"#PSPO-2025"}""",
                    """{"InvoiceID":"5002","Type":"ACCREC","Status":"PAID","Reference":"PRIVATE-2025"}"""
                });

            var collector = new XeroCollector(mockApi.Object, NullLogger<XeroCollector>.Instance);

            var result = await collector.CollectAsync(null);

            result.Should().HaveCount(1, "public invoices starting with '#' should be ignored");
            result.First().Should().Contain("PRIVATE-2025");
        }

        [Test]
        public async Task CollectAsync_ShouldSkipMalformedJson_WithoutThrowing()
        {
            var mockApi = new Mock<IXeroApiClient>();
            mockApi.Setup(x => x.FetchInvoicesAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>
                {
                    """{"InvoiceID":"6001","Type":"ACCREC","Status":"PAID"}""",
                    """{bad json here}"""
                });

            var collector = new XeroCollector(mockApi.Object, NullLogger<XeroCollector>.Instance);

            var result = await collector.CollectAsync(null);

            result.Should().HaveCount(1, "one valid invoice and one malformed JSON string");
        }
    }
}
