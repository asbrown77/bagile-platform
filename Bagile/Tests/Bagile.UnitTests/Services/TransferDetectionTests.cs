using Bagile.EtlService.Helpers;
using Bagile.EtlService.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Bagile.UnitTests.Services;

[TestFixture]
public class TransferDetectionTests
{
    [TestCase("Transfer from cancelled PSM-061125-CB", true, "PSM-061125-CB", TransferReason.CourseCancelled, true)]
    [TestCase("Transfer from PSM-061125-CB", true, "PSM-061125-CB", TransferReason.AttendeeRequested, false)]
    [TestCase("transfer from cancelled psm-061125-cb", true, "psm-061125-cb", TransferReason.CourseCancelled, true)]
    [TestCase("Some other text", false, "", TransferReason.Unknown, false)]
    [TestCase(null, false, "", TransferReason.Unknown, false)]
    [TestCase("", false, "", TransferReason.Unknown, false)]
    public void ParseDesignation_ShouldDetectTransferCorrectly(
        string designation,
        bool expectedIsTransfer,
        string expectedSku,
        TransferReason expectedReason,
        bool expectedRefund)
    {
        // Act
        var result = TransferParser.ParseDesignation(designation);

        // Assert
        result.IsTransfer.Should().Be(expectedIsTransfer);
        if (expectedIsTransfer)
        {
            result.OriginalSku.Should().Be(expectedSku);
            result.Reason.Should().Be(expectedReason);
            result.RefundEligible.Should().Be(expectedRefund);
        }
    }
}