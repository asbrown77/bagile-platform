import type { ITrelloPort, TrelloCardSummary } from "../../../domain/ports/ITrelloPort.js";
import type { IBagileApiPort, PendingTransfer, CourseAtRisk } from "../../../domain/ports/IBagileApiPort.js";
import type { IGmailPort, EmailSummary } from "../../../domain/ports/IGmailPort.js";
import type { ICalendarPort, CalendarEvent } from "../../../domain/ports/ICalendarPort.js";
import type { MorningBriefResult } from "../../dtos/MorningBriefResult.js";

export interface MorningBriefOptions {
  date: string;
  user: string;
  boardId: string;
  daysAhead?: number;
}

interface BagileData {
  transfers: PendingTransfer[];
  coursesAtRisk: CourseAtRisk[];
}

export class MorningBriefUseCase {
  constructor(
    private readonly trello: ITrelloPort,
    private readonly bagile: IBagileApiPort,
    private readonly gmail: IGmailPort,
    private readonly calendar: ICalendarPort
  ) {}

  private async fetchBagileData(daysAhead: number): Promise<BagileData> {
    const [transfers, coursesAtRisk] = await Promise.all([
      this.bagile.getPendingTransfers(),
      this.bagile.getCoursesAtRisk(daysAhead),
    ]);
    return { transfers, coursesAtRisk };
  }

  async execute(options: MorningBriefOptions): Promise<MorningBriefResult> {
    const { date, user, boardId, daysAhead = 30 } = options;
    const warnings: string[] = [];

    const bagilePromise = this.fetchBagileData(daysAhead);

    const [trelloResult, bagileResult, emailsResult, calendarResult] =
      await Promise.allSettled([
        this.trello.getOpenCards(boardId),
        bagilePromise,
        this.gmail.getRecentEmails({ mailbox: "info@bagile.co.uk", days: 3 }),
        this.calendar.getTodayEvents(),
      ]);

    const cards = extractOrWarn<TrelloCardSummary[]>(trelloResult, "Trello unavailable", warnings);
    const bagileData = extractOrWarn<BagileData>(
      bagileResult, "BAgile API unavailable", warnings,
      { transfers: [], coursesAtRisk: [] }
    );
    const transfers = bagileData?.transfers ?? [];
    const atRisk = bagileData?.coursesAtRisk ?? [];
    const emails = extractOrWarn<EmailSummary[]>(emailsResult, "Gmail unavailable", warnings);
    const events = extractOrWarn<CalendarEvent[]>(calendarResult, "Calendar unavailable", warnings);

    return {
      date,
      generatedAt: new Date().toISOString(),
      user,
      status: warnings.length === 0 ? "ok" : "degraded",
      warnings,
      trello: {
        cardCount: cards.length,
        cards,
      },
      transfers: {
        pendingCount: transfers.length,
        items: transfers,
      },
      coursesAtRisk: {
        count: atRisk.length,
        items: atRisk,
      },
      emails: {
        note: "Use Gmail MCP tools to fetch email context",
        items: emails,
      },
      calendar: {
        note: "Use Calendar MCP tools to fetch today's events",
        items: events,
      },
    };
  }
}

function extractOrWarn<T>(
  result: PromiseSettledResult<T>,
  warningMessage: string,
  warnings: string[],
  fallback?: T
): T {
  if (result.status === "fulfilled") {
    return result.value;
  }
  warnings.push(warningMessage);
  return fallback ?? ([] as unknown as T);
}
