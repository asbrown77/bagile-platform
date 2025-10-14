using System.Text.Json;
using Bagile.Infrastructure.Models;
using FluentAssertions;
using NUnit.Framework;
using System.Text.Json.Serialization;

namespace Bagile.UnitTests.Infrastructure;

[TestFixture]
public class NullableDecimalConverterTests
{
    [TestCase("\"\"", null)]
    [TestCase("null", null)]
    [TestCase("\"100.5\"", 100.5)]
    [TestCase("100.5", 100.5)]
    public void Should_Handle_Empty_And_Null_Strings(string jsonFragment, decimal? expected)
    {
        var json = $"{{\"price\": {jsonFragment}}}";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var result = JsonSerializer.Deserialize<TestDto>(json, options);

        result.Should().NotBeNull();
        result!.Price.Should().Be(expected);
    }

    private class TestDto
    {
        [JsonConverter(typeof(NullableDecimalConverter))]
        public decimal? Price { get; set; }
    }

    [Test]
    public void Should_Use_Converter_Directly()
    {
        var json = "\"100.5\"";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DecimalConverterFactory());

        var result = JsonSerializer.Deserialize<decimal?>(json, options);
        result.Should().Be(100.5m);
    }
}