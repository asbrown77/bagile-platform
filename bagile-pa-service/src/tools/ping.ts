export interface PingResult {
  status: "ok";
  user: string;
}

export async function handlePing(): Promise<PingResult> {
  return {
    status: "ok",
    user: process.env.PA_USER_ID ?? "unknown",
  };
}
