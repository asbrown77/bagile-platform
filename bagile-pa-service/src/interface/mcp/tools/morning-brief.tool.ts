import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { MorningBriefUseCase } from "../../../application/use-cases/morning-brief/MorningBriefUseCase.js";
import { TrelloAdapter } from "../../../infrastructure/adapters/trello/TrelloAdapter.js";
import { BagileApiAdapter } from "../../../infrastructure/adapters/bagile-api/BagileApiAdapter.js";
import { GmailStubAdapter } from "../../../infrastructure/adapters/gmail/GmailStubAdapter.js";
import { CalendarStubAdapter } from "../../../infrastructure/adapters/calendar/CalendarStubAdapter.js";
import { buildCredentialResolver } from "../../../infrastructure/credentials/buildCredentialResolver.js";

export function registerMorningBrief(server: McpServer): void {
  const bagileUrl = process.env.BAGILE_API_URL ?? "https://api.bagile.co.uk";
  const bagileKey = process.env.BAGILE_API_KEY ?? "";
  const boardId = process.env.TRELLO_BOARD_ID ?? "hNs49hi4";
  const userId = process.env.PA_USER_ID ?? "unknown";
  const tenantId = process.env.PA_TENANT_ID ?? "bagile";
  const daysAhead = Number(process.env.PA_DAYS_AHEAD ?? "30");

  server.tool(
    "pa_morning_brief",
    "Get the morning brief — Trello CRM cards needing attention, pending transfers, courses at risk. For email and calendar context, also call the Gmail and Calendar MCP tools.",
    {
      date: z.string().optional().describe("Date to brief for (YYYY-MM-DD, defaults to today)"),
    },
    async ({ date }) => {
      const briefDate = date ?? new Date().toISOString().slice(0, 10);

      const resolver = buildCredentialResolver(userId, tenantId);
      const trelloKey = (await resolver("trello_api_key")) ?? "";
      const trelloToken = (await resolver("trello_token")) ?? "";

      const useCase = new MorningBriefUseCase(
        new TrelloAdapter(trelloKey, trelloToken),
        new BagileApiAdapter(bagileUrl, bagileKey),
        new GmailStubAdapter(),
        new CalendarStubAdapter()
      );

      const result = await useCase.execute({
        date: briefDate,
        user: userId,
        boardId,
        daysAhead,
      });

      return {
        content: [{ type: "text" as const, text: JSON.stringify(result, null, 2) }],
      };
    }
  );
}
