import { describe, it, expect, afterEach } from "vitest";
import { handlePing } from "./ping.js";

describe("handlePing", () => {
  afterEach(() => {
    delete process.env.PA_USER_ID;
  });

  it("returns ok status with configured user", async () => {
    process.env.PA_USER_ID = "alex";
    const result = await handlePing();
    expect(result.status).toBe("ok");
    expect(result.user).toBe("alex");
  });

  it("defaults to 'unknown' when PA_USER_ID is not set", async () => {
    const result = await handlePing();
    expect(result.status).toBe("ok");
    expect(result.user).toBe("unknown");
  });
});
