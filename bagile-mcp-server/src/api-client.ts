import { config } from "dotenv";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
config({ path: resolve(__dirname, "../.env") });

const API_URL = process.env.BAGILE_API_URL || "https://api.bagile.co.uk";
const API_KEY: string = process.env.BAGILE_API_KEY ?? (() => {
  throw new Error("BAGILE_API_KEY environment variable is required");
})();

export interface ApiResponse<T> {
  ok: boolean;
  status: number;
  data?: T;
  error?: string;
}

export async function apiGet<T = unknown>(
  path: string,
  params?: Record<string, string | number | undefined>
): Promise<ApiResponse<T>> {
  const url = new URL(path, API_URL);

  if (params) {
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null && value !== "") {
        url.searchParams.set(key, String(value));
      }
    }
  }

  try {
    const response = await fetch(url.toString(), {
      method: "GET",
      headers: {
        "X-Api-Key": API_KEY,
        Accept: "application/json",
      },
    });

    if (!response.ok) {
      return {
        ok: false,
        status: response.status,
        error: `HTTP ${response.status}: ${response.statusText}`,
      };
    }

    const contentType = response.headers.get("content-type") || "";
    const data = contentType.includes("application/json")
      ? ((await response.json()) as T)
      : ((await response.text()) as unknown as T);
    return { ok: true, status: response.status, data };
  } catch (err) {
    return {
      ok: false,
      status: 0,
      error: err instanceof Error ? err.message : String(err),
    };
  }
}
