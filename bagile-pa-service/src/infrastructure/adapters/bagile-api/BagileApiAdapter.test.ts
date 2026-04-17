import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { BagileApiAdapter } from "./BagileApiAdapter.js";

function makeResponse(status: number, body: unknown, statusText = "OK"): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText,
    json: async () => body,
    headers: {
      get: (name: string) =>
        name.toLowerCase() === "content-type" ? "application/json" : null,
    },
  } as unknown as Response;
}

const API_URL = "https://test.bagile.co.uk";
const API_KEY = "test-api-key";

describe("BagileApiAdapter", () => {
  let adapter: BagileApiAdapter;

  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
    adapter = new BagileApiAdapter(API_URL, API_KEY);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("calls correct endpoint with X-Api-Key header for pending transfers", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, []));

    await adapter.getPendingTransfers();

    const calledUrl = vi.mocked(fetch).mock.calls[0][0] as string;
    expect(calledUrl).toBe(`${API_URL}/api/transfers/pending`);

    const calledInit = vi.mocked(fetch).mock.calls[0][1] as RequestInit;
    expect((calledInit.headers as Record<string, string>)["X-Api-Key"]).toBe(API_KEY);
  });

  it("maps pending transfers correctly", async () => {
    const apiResponse = [
      {
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
      },
    ];

    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, apiResponse));

    const result = await adapter.getPendingTransfers();

    expect(result).toEqual(apiResponse);
    expect(result[0].studentName).toBe("Jane Smith");
    expect(result[0].daysSinceCancellation).toBe(12);
  });

  it("calls correct monitoring endpoint with daysAhead param", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, []));

    await adapter.getCoursesAtRisk(14);

    const calledUrl = vi.mocked(fetch).mock.calls[0][0] as string;
    expect(calledUrl).toBe(`${API_URL}/api/course-schedules/monitoring?daysAhead=14`);

    const calledInit = vi.mocked(fetch).mock.calls[0][1] as RequestInit;
    expect((calledInit.headers as Record<string, string>)["X-Api-Key"]).toBe(API_KEY);
  });

  it("maps monitoring results to CourseAtRisk", async () => {
    const apiResponse = [
      {
        id: 16,
        courseCode: "PALE-200426-AB",
        title: "Professional Agile Leadership Essentials™ - 20-21 Apr 26",
        startDate: "2026-04-20T00:00:00",
        currentEnrolmentCount: 2,
        minimumRequired: 3,
        daysUntilDecision: 1,
        recommendedAction: "Push for bookings — 2/3, 1 days to decide",
        monitoringStatus: "at_risk",
      },
    ];

    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, apiResponse));

    const result = await adapter.getCoursesAtRisk(30);

    expect(result).toHaveLength(1);
    expect(result[0]).toEqual({
      courseId: 16,
      courseCode: "PALE-200426-AB",
      title: "Professional Agile Leadership Essentials™ - 20-21 Apr 26",
      startDate: "2026-04-20T00:00:00",
      currentEnrolmentCount: 2,
      minimumRequired: 3,
      daysUntilDecision: 1,
      recommendedAction: "Push for bookings — 2/3, 1 days to decide",
      monitoringStatus: "at_risk",
    });
  });
});
