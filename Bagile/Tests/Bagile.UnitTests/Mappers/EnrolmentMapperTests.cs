using Bagile.EtlService.Mappers;
using Bagile.Domain.Entities;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace Bagile.Tests.Mappers
{
    [TestFixture]
    public class EnrolmentMapperTests
    {
        private string _wooJson = string.Empty;

        [SetUp]
        public void Setup()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            _wooJson = File.ReadAllText(Path.Combine(testDir, "SampleData", "woo_order.json"));
        }

        [Test]
        public void MapFromWooOrder_ShouldCreateEnrolmentsForEachTicket()
        {
            // Arrange
            long orderId = 501;
            long studentId = 999; // fake ID for test

            // Act
            var enrolments = EnrolmentMapper.MapFromWooOrder(_wooJson, orderId, studentId).ToList();

            // Assert
            Assert.That(enrolments, Is.Not.Null);
            Assert.That(enrolments.Count, Is.EqualTo(2)); // Adjust based on actual sample data
            var first = enrolments.First();

            Assert.That(first.OrderId, Is.EqualTo(orderId));
            Assert.That(first.StudentId, Is.EqualTo(studentId));
            Assert.That(first.CourseScheduleId, Is.Null);
        }

        [Test]
        public void MapFromWooOrder_ShouldHandleMissingTicketsGracefully()
        {
            var payload = "{\"id\": 1001, \"total\": \"0\"}";
            var enrolments = EnrolmentMapper.MapFromWooOrder(payload, 99, 123).ToList();

            Assert.That(enrolments, Is.Not.Null);
            Assert.That(enrolments.Count, Is.EqualTo(0));
        }
    }
}