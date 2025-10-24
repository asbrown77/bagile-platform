using Bagile.EtlService.Mappers;
using NUnit.Framework;
using System.IO;

namespace Bagile.Tests.Mappers
{
    [TestFixture]
    public class StudentMapperTests
    {
        private string _wooJson = string.Empty;

        [SetUp]
        public void Setup()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            _wooJson = File.ReadAllText(Path.Combine(testDir, "SampleData", "woo_order_with_tickets.json"));
        }

        [Test]
        public void MapFromWooOrder_ShouldExtractAllAttendees()
        {
            var students = StudentMapper.MapFromWooOrder(_wooJson).ToList();

            Assert.That(students, Is.Not.Empty);
            Assert.That(students.Count, Is.EqualTo(4)); // based on sample
            Assert.That(students.First().Email, Does.Contain("@"));
            Assert.That(students.First().FirstName, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void MapFromWooOrder_ShouldSkipBlankEmails()
        {
            var json = "{\"meta_data\": [{\"key\": \"WooCommerceEventsOrderTickets\", \"value\": {\"1\": {\"1\": {\"WooCommerceEventsAttendeeEmail\": \"\"}}}}]}";
            var students = StudentMapper.MapFromWooOrder(json).ToList();
            Assert.That(students.Count, Is.EqualTo(0));
        }
    }
}