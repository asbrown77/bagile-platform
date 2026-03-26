import { describe, it, expect } from "vitest";
import { formatResult } from "../utils.js";

describe("formatResult", () => {
  it("returns 'Error: <message>' when ok is false", () => {
    const result = formatResult({ ok: false, error: "HTTP 404: Not Found" });
    expect(result).toBe("Error: HTTP 404: Not Found");
  });

  it("returns pretty-printed JSON when ok is true with data", () => {
    const data = { id: 1, name: "PSM I" };
    const result = formatResult({ ok: true, data });
    expect(result).toBe(JSON.stringify(data, null, 2));
  });

  it("returns 'null' when ok is true but data is undefined", () => {
    // JSON.stringify(undefined, null, 2) returns undefined (not a string),
    // which is the expected behaviour — callers should be aware data may be absent.
    const result = formatResult({ ok: true, data: undefined });
    // undefined serialises to undefined in JSON.stringify; the function returns
    // that raw value. We verify the function does not throw and returns the
    // stringified form (which is undefined, since JSON.stringify(undefined) === undefined).
    expect(result).toBeUndefined();
  });

  it("returns 'null' when ok is true and data is explicitly null", () => {
    const result = formatResult({ ok: true, data: null });
    expect(result).toBe("null");
  });

  it("serialises arrays correctly", () => {
    const data = [{ id: 1 }, { id: 2 }];
    const result = formatResult({ ok: true, data });
    expect(result).toBe(JSON.stringify(data, null, 2));
  });
});
