using System.Text.RegularExpressions;
using Bagile.EtlService.Models;

namespace Bagile.EtlService.Helpers;

public static class TransferParser
{
    public static TransferInfo ParseDesignation(string? designation)
    {
        if (string.IsNullOrWhiteSpace(designation))
            return new TransferInfo { IsTransfer = false };

        var lowerDesignation = designation.ToLower();

        if (!lowerDesignation.Contains("transfer from"))
            return new TransferInfo { IsTransfer = false };

        var info = new TransferInfo { IsTransfer = true };

        // Check if course was cancelled (refund eligible)
        if (lowerDesignation.Contains("cancelled"))
        {
            info.Reason = TransferReason.CourseCancelled;
            info.RefundEligible = true;

            var match = Regex.Match(designation,
                @"transfer from cancelled\s+([A-Z0-9\-]+)",
                RegexOptions.IgnoreCase);

            if (match.Success)
                info.OriginalSku = match.Groups[1].Value;
        }
        else
        {
            info.Reason = TransferReason.AttendeeRequested;
            info.RefundEligible = false;

            var match = Regex.Match(designation,
                @"transfer from\s+([A-Z0-9\-]+)",
                RegexOptions.IgnoreCase);

            if (match.Success)
                info.OriginalSku = match.Groups[1].Value;
        }

        return info;
    }
}