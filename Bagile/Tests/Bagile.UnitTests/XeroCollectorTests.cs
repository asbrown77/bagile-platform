using System.Collections.Generic;
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
            // arrange
            var mockApi = new Mock<IXeroApiClient>();
            mockApi.Setup(x => x.FetchInvoicesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>
                {
                    """{"id":1001,"Status":"PAID"}""",
                    """{"id":2002,"Status":"AUTHORISED"}"""
                });


            var collector = new XeroCollector(mockApi.Object, NullLogger<XeroCollector>.Instance);

            // act
            var result = await collector.CollectAsync();

            // assert
            result.Should().HaveCount(2);
            result.Should().Contain(r => r.Contains("1001"));
            result.Should().Contain(r => r.Contains("2002"));
            mockApi.Verify(x => x.FetchInvoicesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void SourceName_ShouldBeXero()
        {
            var collector = new XeroCollector(Mock.Of<IXeroApiClient>(), NullLogger<XeroCollector>.Instance);
            collector.SourceName.Should().Be("xero");
        }
    }
}