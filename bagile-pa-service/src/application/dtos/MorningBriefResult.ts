import type { TrelloCardSummary } from "../../domain/ports/ITrelloPort.js";
import type { PendingTransfer, CourseAtRisk } from "../../domain/ports/IBagileApiPort.js";
import type { EmailSummary } from "../../domain/ports/IGmailPort.js";
import type { CalendarEvent } from "../../domain/ports/ICalendarPort.js";

export interface MorningBriefResult {
  date: string;
  generatedAt: string;
  user: string;
  status: "ok" | "degraded";
  warnings: string[];
  trello: {
    cardCount: number;
    cards: TrelloCardSummary[];
  };
  transfers: {
    pendingCount: number;
    items: PendingTransfer[];
  };
  coursesAtRisk: {
    count: number;
    items: CourseAtRisk[];
  };
  emails: {
    note: string;
    items: EmailSummary[];
  };
  calendar: {
    note: string;
    items: CalendarEvent[];
  };
}
