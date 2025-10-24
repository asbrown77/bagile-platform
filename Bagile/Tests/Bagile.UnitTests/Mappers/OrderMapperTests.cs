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

            // Woo: ExternalId = "id", Reference = "number"
            Assert.That(order.ExternalId, Is.EqualTo("12243"));
            Assert.That(order.Reference, Is.EqualTo("12243"));

            Assert.That(order.TotalAmount, Is.EqualTo(2520m));
            Assert.That(order.Status, Is.EqualTo("completed")); // normalized now
            Assert.That(order.BillingCompany, Is.EqualTo("MDU Services Ltd"));
            Assert.That(order.ContactName, Is.EqualTo("Henry Heselden"));
            Assert.That(order.ContactEmail, Is.EqualTo("henry.heselden@themdu.com"));
            Assert.That(order.OrderDate, Is.Not.Null);
        }

        [Test]
        public void MapFromRaw_XeroInvoice_ShouldMapCoreFieldsCorrectly()
        {
            var order = OrderMapper.MapFromRaw("xero", 102, _xeroJson);

            Assert.That(order, Is.Not.Null);
            Assert.That(order!.Source, Is.EqualTo("xero"));
            Assert.That(order.Type, Is.EqualTo("private"));

            // Matches your JSON
            Assert.That(order.ExternalId, Is.EqualTo("INV-0001"));
            Assert.That(order.Reference, Is.EqualTo("PT-PSPO-001"));
            Assert.That(order.TotalAmount, Is.EqualTo(14400m));
            Assert.That(order.Status, Is.EqualTo("completed")); // normalized from "PAID"
            Assert.That(order.BillingCompany, Is.EqualTo("Checkatrade"));
            Assert.That(order.ContactEmail, Is.Null);
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
            Assert.That(order.Status, Is.EqualTo("pending")); // default normalization fallback
        }
    }
}
