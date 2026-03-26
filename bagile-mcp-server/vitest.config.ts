import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "node",
    // Run tests in a forked process so ESM + import.meta work correctly
    pool: "forks",
  },
  resolve: {
    // Match TypeScript "moduleResolution: bundler" — prefer ESM exports
    conditions: ["import", "module", "default"],
  },
});
