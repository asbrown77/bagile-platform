"use client";

import { useEffect, useState } from "react";

// null  = still reading from localStorage (don't fire API calls yet)
// ""    = read complete, key not found (redirect to login)
// "..." = key ready
export function useApiKey(): string | null {
  const [apiKey, setApiKey] = useState<string | null>(null);

  useEffect(() => {
    const key = process.env.NEXT_PUBLIC_BAGILE_API_KEY
      || localStorage.getItem("bagile_api_key")
      || "";
    setApiKey(key);
  }, []);

  return apiKey;
}
