using System.IO;
using System.Text.Json;
using Bagile.Infrastructure.Mappers;
using Bagile.Infrastructure.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Bagile.UnitTests.Infrastructure
{
    [TestFixture]
    public class WooMapperTests
    {
        private JsonSerializerOptions _jsonOptions = null!;

        [SetUp]
        public void Setup()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [Test]
        public async Task MapToCourseSchedule_ShouldMapExpectedFields()
        {
            // Arrange
            var path = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "SampleData",
                "woo_product_10460.json"
            );

            File.Exists(path).Should().BeTrue($"expected test data file at {path}");

            var json = await File.ReadAllTextAsync(path);
            var productDto = JsonSerializer.Deserialize<WooProductDto>(json, _jsonOptions);
            productDto.Should().NotBeNull("the Woo product JSON should deserialize correctly");

            // Act
            var result = WooMapper.ToCourseSchedule(productDto);

            // Assert
            result.Should().NotBeNull();
            result.SourceProductId.Should().Be(10460);
            result.Name.Should().Be("Professional Scrum with User Experience - 6-7 Feb 25 (Template)");
            result.Sku.Should().Be("PSU-060225-AB-1");
            result.Price.Should().Be(1050);
           // result.TrainerName.Should().Be("");
            result.FormatType.Should().Be("Live virtual training");
            result.StartDate.Should().Be(DateTime.Parse("2025-02-06"));
            result.EndDate.Should().Be(DateTime.Parse("2025-02-07"));
            result.SourceSystem.Should().Be("WooCommerce");
        }
    }
}
