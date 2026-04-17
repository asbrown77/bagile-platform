import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { handlePing } from "./tools/ping.js";
import { registerMorningBrief } from "./interface/mcp/tools/morning-brief.tool.js";
import { registerPaTasks } from "./interface/mcp/tools/pa-tasks.tool.js";
import { registerHealthStatus } from "./interface/mcp/tools/health-status.tool.js";
import { registerTransferFooEventTicket } from "./interface/mcp/tools/transfer-fooevent-ticket.tool.js";
import { registerUpdateTrelloCard } from "./interface/mcp/tools/update-trello-card.tool.js";
import { registerCancelCourse } from "./interface/mcp/tools/cancel-course.tool.js";
import { registerLookupXeroInvoice } from "./interface/mcp/tools/lookup-xero-invoice.tool.js";
import { registerLabelGmailDraft } from "./interface/mcp/tools/label-gmail-draft.tool.js";
import { registerCreateScrumOrgCourse } from "./interface/mcp/tools/create-scrumorg-course.tool.js";
import { registerCredentials } from "./interface/mcp/tools/credentials.tool.js";

export function createServer(): McpServer {
  const server = new McpServer({
    name: "bagile-pa",
    version: "1.0.0",
  });

  server.tool(
    "pa_ping",
    "Health check — confirms the BAgile PA service is running and returns the active user",
    {},
    async () => {
      const result = await handlePing();
      return { content: [{ type: "text" as const, text: JSON.stringify(result) }] };
    }
  );

  registerMorningBrief(server);
  registerPaTasks(server);
  registerHealthStatus(server);
  registerTransferFooEventTicket(server);
  registerUpdateTrelloCard(server);
  registerCancelCourse(server);
  registerLookupXeroInvoice(server);
  registerLabelGmailDraft(server);
  registerCreateScrumOrgCourse(server);
  registerCredentials(server);

  return server;
}
