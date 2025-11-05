using System.IO;
using System.Linq;
using Bagile.EtlService.Mappers;
using NUnit.Framework;

namespace Bagile.Tests.Mappers
{
    [TestFixture]
    public class WooOrderTicketMapperTests
    {
        private string _wooJson = string.Empty;

        [SetUp]
        public void Setup()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            _wooJson = File.ReadAllText(Path.Combine(testDir, "SampleData", "woo_order_with_tickets.json"));
        }

        [Test]
        public void MapTickets_ShouldExtractAllTickets()
        {
            var tickets = WooOrderTicketMapper.MapTickets(_wooJson).ToList();

            Assert.That(tickets, Is.Not.Empty);
            Assert.That(tickets.Count, Is.EqualTo(4)); // based on sample data
            Assert.That(tickets.First().Email, Does.Contain("@"));
            Assert.That(tickets.First().ProductId, Is.Not.Null);
        }

        [Test]
        public void MapTickets_ShouldSkipBlankEmails()
        {
            var json =
                """
                {
                  "meta_data": [
                    {
                      "key": "WooCommerceEventsOrderTickets",
                      "value": {
                        "1": {
                          "1": {
                            "WooCommerceEventsAttendeeEmail": ""
                          }
                        }
                      }
                    }
                  ]
                }
                """;

            var tickets = WooOrderTicketMapper.MapTickets(json).ToList();

            Assert.That(tickets.Count, Is.EqualTo(0));
        }
    }
}