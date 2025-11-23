using System.Text.Json;

namespace Bagile.EtlService.Helpers
{
    public static class WooOrderTransferHelper
    {
        public static bool IsInternalTransferOrder(string payload)
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                if (!root.TryGetProperty("billing", out var billing))
                    return false;

                if (!billing.TryGetProperty("company", out var companyProp))
                    return false;

                var company = companyProp.GetString()?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(company))
                    return false;

                return company.ToLower() == "b-agile"
                       || company == "bagile"
                       || company == "bagile limited";
            }
            catch
            {
                return false;
            }
        }
    }
}