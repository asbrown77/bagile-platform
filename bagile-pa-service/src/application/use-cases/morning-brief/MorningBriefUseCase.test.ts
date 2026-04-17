import { describe, it, expect, vi } from "vitest";
import { MorningBriefUseCase } from "./MorningBriefUseCase.js";
import type { ITrelloPort, TrelloCardSummary } from "../../../domain/ports/ITrelloPort.js";
import type { IBagileApiPort, PendingTransfer, CourseAtRisk } from "../../../domain/ports/IBagileApiPort.js";
import type { IGmailPort } from "../../../domain/ports/IGmailPort.js";
import type { ICalendarPort } from "../../../domain/ports/ICalendarPort.js";

function makeTrelloPort(cards: TrelloCardSummary[] = []): ITrelloPort {
  return { getOpenCards: vi.fn().mockResolvedValue(cards) };
}

function makeBagilePort(
  transfers: PendingTransfer[] = [],
  atRisk: CourseAtRisk[] = []
): IBagileApiPort {
  return {
    getPendingTransfers: vi.fn().mockResolvedValue(transfers),
    getCoursesAtRisk: vi.fn().mockResolvedValue(atRisk),
  };
}

function makeGmailPort(): IGmailPort {
  return { getRecentEmails: vi.fn().mockResolvedValue([]) };
}

function makeCalendarPort(): ICalendarPort {
  return { getTodayEvents: vi.fn().mockResolvedValue([]) };
}

const DEFAULT_OPTIONS = {
  date: "2026-04-16",
  user: "alex",
  boardId: "hNs49hi4",
  daysAhead: 30,
};

const SAMPLE_CARD: TrelloCardSummary = {
  id: "c1",
  name: "Acme — Jane",
  listName: "Incoming",
  dueDate: "2026-04-20T00:00:00.000Z",
  isOverdue: false,
  url: "https://trello.com/c/c1",
};

const SAMPLE_TRANSFER: PendingTransfer = {
  studentId: 42,
  studentName: "Jane Smith",
  studentEmail: "jane@example.com",
  organisation: "Acme Corp",
  cancelledScheduleId: 7,
  courseCode: "PSM",
  courseTitle: "Professional Scrum Master™",
  originalStartDate: "2026-03-15T00:00:00",
  cancelledDate: "2026-03-10T00:00:00",
  daysSinceCancellation: 12,
};

const SAMPLE_AT_RISK: CourseAtRisk = {
  courseId: 16,
  courseCode: "PALE-200426-AB",
  title: "Professional Agile Leadership Essentials™ - 20-21 Apr 26",
  startDate: "2026-04-20T00:00:00",
  currentEnrolmentCount: 2,
  minimumRequired: 3,
  daysUntilDecision: 1,
  recommendedAction: "Push for bookings — 2/3, 1 days to decide",
  monitoringStatus: "at_risk",
};

describe("MorningBriefUseCase", () => {
  it("returns ok status with data when all ports succeed", async () => {
    const useCase = new MorningBriefUseCase(
      makeTrelloPort([SAMPLE_CARD]),
      makeBagilePort([SAMPLE_TRANSFER], [SAMPLE_AT_RISK]),
      makeGmailPort(),
      makeCalendarPort()
    );

    const result = await useCase.execute(DEFAULT_OPTIONS);

    expect(result.status).toBe("ok");
    expect(result.warnings).toEqual([]);
    expect(result.date).toBe("2026-04-16");
    expect(result.user).toBe("alex");
    expect(result.trello.cardCount).toBe(1);
    expect(result.trello.cards).toEqual([SAMPLE_CARD]);
    expect(result.transfers.pendingCount).toBe(1);
    expect(result.transfers.items).toEqual([SAMPLE_TRANSFER]);
    expect(result.coursesAtRisk.count).toBe(1);
    expect(result.coursesAtRisk.items).toEqual([SAMPLE_AT_RISK]);
    expect(result.emails.note).toContain("Gmail MCP");
    expect(result.calendar.note).toContain("Calendar MCP");
  });

  it("returns degraded status when Trello fails", async () => {
    const trello: ITrelloPort = {
      getOpenCards: vi.fn().mockRejectedValue(new Error("Trello timeout")),
    };

    const useCase = new MorningBriefUseCase(
      trello,
      makeBagilePort([SAMPLE_TRANSFER], [SAMPLE_AT_RISK]),
      makeGmailPort(),
      makeCalendarPort()
    );

    const result = await useCase.execute(DEFAULT_OPTIONS);

    expect(result.status).toBe("degraded");
    expect(result.warnings).toHaveLength(1);
    expect(result.warnings[0]).toContain("Trello");
    expect(result.trello.cardCount).toBe(0);
    expect(result.trello.cards).toEqual([]);
    // Other sections still populated
    expect(result.transfers.pendingCount).toBe(1);
    expect(result.coursesAtRisk.count).toBe(1);
  });

  it("returns degraded status when BAgile API fails", async () => {
    const bagile: IBagileApiPort = {
      getPendingTransfers: vi.fn().mockRejectedValue(new Error("API down")),
      getCoursesAtRisk: vi.fn().mockRejectedValue(new Error("API down")),
    };

    const useCase = new MorningBriefUseCase(
      makeTrelloPort([SAMPLE_CARD]),
      bagile,
      makeGmailPort(),
      makeCalendarPort()
    );

    const result = await useCase.execute(DEFAULT_OPTIONS);

    expect(result.status).toBe("degraded");
    expect(result.warnings.some((w) => w.includes("BAgile API"))).toBe(true);
    expect(result.transfers.pendingCount).toBe(0);
    expect(result.coursesAtRisk.count).toBe(0);
    // Trello still works
    expect(result.trello.cardCount).toBe(1);
  });

  it("returns degraded with 4 warnings when all ports fail, never throws", async () => {
    const trello: ITrelloPort = {
      getOpenCards: vi.fn().mockRejectedValue(new Error("fail")),
    };
    const bagile: IBagileApiPort = {
      getPendingTransfers: vi.fn().mockRejectedValue(new Error("fail")),
      getCoursesAtRisk: vi.fn().mockRejectedValue(new Error("fail")),
    };
    const gmail: IGmailPort = {
      getRecentEmails: vi.fn().mockRejectedValue(new Error("fail")),
    };
    const calendar: ICalendarPort = {
      getTodayEvents: vi.fn().mockRejectedValue(new Error("fail")),
    };

    const useCase = new MorningBriefUseCase(trello, bagile, gmail, calendar);

    const result = await useCase.execute(DEFAULT_OPTIONS);

    expect(result.status).toBe("degraded");
    expect(result.warnings).toHaveLength(4);
    expect(result.trello.cards).toEqual([]);
    expect(result.transfers.items).toEqual([]);
    expect(result.coursesAtRisk.items).toEqual([]);
    expect(result.emails.items).toEqual([]);
    expect(result.calendar.items).toEqual([]);
  });
});
