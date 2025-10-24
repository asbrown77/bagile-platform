using Bagile.EtlService.Mappers;
using NUnit.Framework;
using System;
using System.IO;

namespace Bagile.Tests.Mappers
{
    [TestFixture]
    public class OrderMapperTests
    {
        private string _wooJson = string.Empty;
        private string _xeroJson = string.Empty;

        [SetUp]
        public void Setup()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            _wooJson = File.ReadAllText(Path.Combine(testDir, "SampleData", "woo_order.json"));
            _xeroJson = File.ReadAllText(Path.Combine(testDir, "SampleData", "xero_invoice_import.json"));
        }

        [Test]
        public void MapFromRaw_WooOrder_ShouldMapCoreFieldsCorrectly()
        {
            var order = OrderMapper.MapFromRaw("woo", 101, _wooJson);

            Assert.That(order, Is.Not.Null);
            Assert.That(order!.Source, Is.EqualTo("woo"));
            Assert.That(order.Type, Is.EqualTo("public"));
            Assert.That(order.ExternalId, Is.EqualTo("12243"));
            Assert.That(order.TotalAmount, Is.EqualTo(2520m));
            Assert.That(order.Status, Is.EqualTo("processing"));
            Assert.That(order.BillingCompany, Is.EqualTo("MDU Services Ltd"));
            Assert.That(order.ContactName, Is.EqualTo("Henry Heselden"));
            Assert.That(order.ContactEmail, Is.EqualTo("henry.heselden@themdu.com"));
            Assert.That(order.Reference, Is.EqualTo("12243"));
            Assert.That(order.OrderDate, Is.Not.Null);
        }

        [Test]
        public void MapFromRaw_XeroInvoice_ShouldMapCoreFieldsCorrectly()
        {
            var order = OrderMapper.MapFromRaw("xero", 102, _xeroJson);

            Assert.That(order, Is.Not.Null);
            Assert.That(order!.Source, Is.EqualTo("xero"));
            Assert.That(order.Type, Is.EqualTo("private"));
            Assert.That(order.ExternalId, Is.EqualTo("afd1dfc3-bcde-47d2-9017-9160467b9a46"));
            Assert.That(order.TotalAmount, Is.EqualTo(23760m));
            Assert.That(order.Status, Is.EqualTo("PAID"));
            Assert.That(order.BillingCompany, Is.EqualTo("DLA Piper UK LLP"));
            Assert.That(order.ContactEmail, Is.Null); // no EmailAddress in your Xero payload
            Assert.That(order.Reference, Is.EqualTo("DLA-ICAgile-280725-BA"));
            Assert.That(order.OrderDate, Is.Not.Null);
        }

        [Test]
        public void MapFromRaw_UnknownSource_ShouldReturnNull()
        {
            var order = OrderMapper.MapFromRaw("random", 103, _wooJson);
            Assert.That(order, Is.Null);
        }

        [Test]
        public void MapFromRaw_ShouldHandleMissingOptionalFieldsGracefully()
        {
            var minimal = "{\"id\": 9999, \"total\": \"0\"}";
            var order = OrderMapper.MapFromRaw("woo", 104, minimal);

            Assert.That(order, Is.Not.Null);
            Assert.That(order!.Type, Is.EqualTo("public"));
            Assert.That(order.TotalAmount, Is.EqualTo(0m));
            Assert.That(order.ContactEmail, Is.Null);
            Assert.That(order.BillingCompany, Is.Null);
        }
    }
}
