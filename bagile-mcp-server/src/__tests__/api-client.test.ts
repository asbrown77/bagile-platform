/**
 * Tests for src/api-client.ts
 *
 * Strategy: mock dotenv (no-op) and stub global fetch before importing the
 * module. Because api-client.ts reads env vars at module load time, we set
 * process.env before the dynamic import so the constants are captured with
 * known values.
 */
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";

// Prevent dotenv from trying to read a .env file during import
vi.mock("dotenv", () => ({
  config: vi.fn(),
}));

// Set known env values before the module is loaded
process.env.BAGILE_API_URL = "https://test.bagile.co.uk";
process.env.BAGILE_API_KEY = "test-api-key";

// Dynamic import so the mock above is in place when the module initialises
const { apiGet } = await import("../api-client.js");

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeResponse(
  status: number,
  body: unknown,
  statusText = "OK"
): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText,
    json: async () => body,
  } as unknown as Response;
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe("apiGet", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns ok: true with parsed data on a 200 response", async () => {
    const payload = { id: 1, name: "Test Course" };
    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, payload));

    const result = await apiGet("/api/course-schedules");

    expect(result.ok).toBe(true);
    expect(result.status).toBe(200);
    expect(result.data).toEqual(payload);
    expect(result.error).toBeUndefined();
  });

  it("returns ok: false with error message on a 404 response", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      makeResponse(404, null, "Not Found")
    );

    const result = await apiGet("/api/course-schedules/9999");

    expect(result.ok).toBe(false);
    expect(result.status).toBe(404);
    expect(result.error).toBe("HTTP 404: Not Found");
    expect(result.data).toBeUndefined();
  });

  it("returns ok: false with error message when fetch throws a network error", async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new Error("Network failure"));

    const result = await apiGet("/api/course-schedules");

    expect(result.ok).toBe(false);
    expect(result.status).toBe(0);
    expect(result.error).toBe("Network failure");
  });

  it("excludes undefined params from the query string", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, []));

    await apiGet("/api/course-schedules", {
      from: "2024-01-01",
      to: undefined,
      courseCode: undefined,
    });

    const calledUrl = vi.mocked(fetch).mock.calls[0][0] as string;
    expect(calledUrl).toContain("from=2024-01-01");
    expect(calledUrl).not.toContain("to=");
    expect(calledUrl).not.toContain("courseCode=");
  });

  it("excludes empty string params from the query string", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, []));

    await apiGet("/api/students", { email: "", name: "Alice" });

    const calledUrl = vi.mocked(fetch).mock.calls[0][0] as string;
    expect(calledUrl).toContain("name=Alice");
    expect(calledUrl).not.toContain("email=");
  });

  it("includes numeric 0 params in the query string (falsy-but-valid value)", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, []));

    await apiGet("/api/course-schedules", { page: 0 });

    const calledUrl = vi.mocked(fetch).mock.calls[0][0] as string;
    expect(calledUrl).toContain("page=0");
  });

  it("always sends the X-Api-Key header", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, {}));

    await apiGet("/health");

    const calledInit = vi.mocked(fetch).mock.calls[0][1] as RequestInit;
    expect((calledInit.headers as Record<string, string>)["X-Api-Key"]).toBe(
      "test-api-key"
    );
  });
});
