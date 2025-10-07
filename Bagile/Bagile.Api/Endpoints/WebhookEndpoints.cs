using Bagile.Api.Handlers;

namespace Bagile.Api.Endpoints
{
    public static class WebhookEndpoints
    {
        public static void MapWebhookEndpoints(this WebApplication app)
        {
            app.MapPost("/webhooks/{source}", async (HttpContext http, string source, WebhookHandler handler)
                => await handler.HandleAsync(http, source));
        }
    }

}
