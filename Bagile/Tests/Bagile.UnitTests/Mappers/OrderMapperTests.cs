using Bagile.EtlService.Mappers;
using Bagile.Domain.Entities;
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
            var raw = new RawOrder { Id = 101, Source = "woo", Payload = _wooJson };
            var order = OrderMapper.MapFromRaw(raw);

            Assert.That(order, Is.Not.Null);
            Assert.That(order!.Source, Is.EqualTo("woo"));
            Assert.That(order.Type, Is.EqualTo("public"));

            Assert.That(order.ExternalId, Is.EqualTo("12243"));
            Assert.That(order.Reference, Is.EqualTo("12243"));
            Assert.That(order.TotalAmount, Is.EqualTo(2520m));
            Assert.That(order.Status, Is.EqualTo("completed"));
            Assert.That(order.BillingCompany, Is.EqualTo("MDU Services Ltd"));
            Assert.That(order.ContactName, Is.EqualTo("Henry Heselden"));
            Assert.That(order.ContactEmail, Is.EqualTo("henry.heselden@themdu.com"));
            Assert.That(order.OrderDate, Is.Not.Null);
        }

        [Test]
        public void MapFromRaw_XeroInvoice_ShouldMapCoreFieldsCorrectly()
        {
            var raw = new RawOrder { Id = 102, Source = "xero", Payload = _xeroJson };
            var order = OrderMapper.MapFromRaw(raw);

            Assert.That(order, Is.Not.Null);
            Assert.That(order!.Source, Is.EqualTo("xero"));
            Assert.That(order.Type, Is.EqualTo("private"));

            Assert.That(order.ExternalId, Is.EqualTo("INV-0001"));
            Assert.That(order.Reference, Is.EqualTo("PT-PSPO-001"));
            Assert.That(order.TotalAmount, Is.EqualTo(14400m));
            Assert.That(order.Status, Is.EqualTo("completed"));
            Assert.That(order.BillingCompany, Is.EqualTo("Checkatrade"));
            Assert.That(order.ContactEmail, Is.Null);
            Assert.That(order.OrderDate, Is.Not.Null);
        }

        [Test]
        public void MapFromRaw_UnknownSource_ShouldReturnNull()
        {
            var raw = new RawOrder { Id = 103, Source = "random", Payload = _wooJson };
            var order = OrderMapper.MapFromRaw(raw);
            Assert.That(order, Is.Null);
        }

        [Test]
        public void MapFromRaw_ShouldHandleMissingOptionalFieldsGracefully()
        {
            // Minimal valid Woo payload. Has required structure but missing optional fields
            var payload = "{\"id\": 9999, \"total\": \"0\", \"line_items\": [], \"billing\": {}}";
            var raw = new RawOrder
            {
                Id = 104,
                Source = "woo",

                Payload = payload

            };

            var order = OrderMapper.MapFromRaw(raw);

            Assert.That(order, Is.Not.Null);

            Assert.That(order!.Source, Is.EqualTo("woo"));
            Assert.That(order.Type, Is.EqualTo("public"));

            // Core numeric mapping still works
            Assert.That(order.TotalAmount, Is.EqualTo(0m));
            Assert.That(order.SubTotal, Is.EqualTo(0m));
            Assert.That(order.TotalTax, Is.EqualTo(0m));

            // Optional fields should be null, not crash
            Assert.That(order.ContactEmail, Is.Null);
            Assert.That(order.BillingCompany, Is.Null);
            Assert.That(order.ContactName, Is.EqualTo(string.Empty).Or.EqualTo(null));

            // No status in payload, so NormalizeStatus should default to pending
            Assert.That(order.Status, Is.EqualTo("pending"));

            // OrderDate falls back to some non default value
            Assert.That(order.OrderDate, Is.Not.EqualTo(default(DateTime)));

            Assert.That(order!.Type, Is.EqualTo("public"));
            Assert.That(order.TotalAmount, Is.EqualTo(0m));
            Assert.That(order.ContactEmail, Is.Null);
            Assert.That(order.BillingCompany, Is.Null);
            Assert.That(order.Status, Is.EqualTo("pending"));

        }
    }
}
