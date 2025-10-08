using Bagile.Api.Services;

namespace Bagile.Api.Endpoints;

public static class XeroAuthSetupEndpoints
{
    public static void MapXeroOAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/xero/connect", (IConfiguration cfg) =>
        {
            var clientId = cfg["Xero:ClientId"];
            var redirectUri = cfg["Xero:RedirectUri"];
            var scope = "offline_access accounting.transactions.read accounting.contacts.read";

            var url = $"https://login.xero.com/identity/connect/authorize" +
                      $"?response_type=code" +
                      $"&client_id={clientId}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}" +
                      $"&scope={Uri.EscapeDataString(scope)}";

            return Results.Redirect(url);
        })
        .WithName("ConnectToXero")
        .WithSummary("Authorises this application to access your Xero organisation.")
        .WithDescription(
            "Starts the Xero OAuth 2.0 authorisation flow. " +
            "Opens the Xero consent screen where you log in and grant access for your organisation. " +
            "Once approved, Xero redirects back to `/xero/callback` with an authorisation code."
        );

        app.MapGet("/xero/callback", async (string code, XeroAuthSetupService xero) =>
        {
            var (access, refresh, expires) = await xero.ExchangeCodeForTokensAsync(code);
            var (tenantId, tenantName) = await xero.GetTenantAsync(access);
            await xero.SaveIntegrationTokenAsync(refresh, access, expires, tenantId);

            return Results.Text($"✅ Xero connected successfully to {tenantName}. You can close this tab.");
        })
        .WithName("XeroCallback")
        .WithSummary("Handles the Xero redirect after authorisation.")
        .WithDescription(
            "Called automatically by Xero after you approve access. " +
            "It exchanges the authorisation code for access and refresh tokens, " +
            "detects the selected organisation, and stores credentials securely in the database."
        );
    }
}