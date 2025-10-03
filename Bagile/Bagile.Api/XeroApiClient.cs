using System.Text.Json;

public class XeroApiClient
{
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public XeroApiClient(IConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<XeroInvoice?> GetInvoiceByIdAsync(string invoiceId)
    {
        var baseUrl = "https://api.xero.com/api.xro/2.0/Invoices";
        var url = $"{baseUrl}/{invoiceId}";

        // you’d need to handle auth (OAuth2 token) here
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var invoiceElement = doc.RootElement
            .GetProperty("Invoices")
            .EnumerateArray()
            .FirstOrDefault();

        return JsonSerializer.Deserialize<XeroInvoice>(invoiceElement.GetRawText());
    }
}