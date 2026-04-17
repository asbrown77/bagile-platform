import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { fetchJson } from "./fetchJson.js";

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
    headers: {
      get: (name: string) =>
        name.toLowerCase() === "content-type" ? "application/json" : null,
    },
  } as unknown as Response;
}

describe("fetchJson", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns parsed JSON on a 200 response", async () => {
    const payload = { id: 1, name: "Test" };
    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, payload));

    const result = await fetchJson<{ id: number; name: string }>("https://example.com/api");

    expect(result).toEqual(payload);
    expect(fetch).toHaveBeenCalledWith("https://example.com/api", undefined);
  });

  it("throws with HTTP status message on non-200", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      makeResponse(403, null, "Forbidden")
    );

    await expect(fetchJson("https://example.com/api")).rejects.toThrow(
      "HTTP 403: Forbidden"
    );
  });

  it("re-throws network errors", async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new Error("DNS resolution failed"));

    await expect(fetchJson("https://example.com/api")).rejects.toThrow(
      "DNS resolution failed"
    );
  });

  it("passes through RequestInit options", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeResponse(200, {}));

    const init: RequestInit = {
      headers: { "X-Api-Key": "secret" },
    };
    await fetchJson("https://example.com/api", init);

    expect(fetch).toHaveBeenCalledWith("https://example.com/api", init);
  });
});
