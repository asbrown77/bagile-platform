namespace Bagile.EtlService.Models;

public class TransferInfo
{
    public bool IsTransfer { get; set; }
    public string OriginalSku { get; set; } = string.Empty;
    public TransferReason Reason { get; set; }
    public bool RefundEligible { get; set; }
}
