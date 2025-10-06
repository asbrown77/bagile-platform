using Bagile.EtlService.Utils;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class JsonHelpersTests
{
    [Test]
    public void ExtractId_Returns_Guid_When_Id_Missing()
    {
        var json = """{"name":"Test"}""";
        var id = JsonHelpers.ExtractId(json);

        Guid.TryParse(id, out _).Should().BeTrue();  // proves it’s a GUID
    }
}