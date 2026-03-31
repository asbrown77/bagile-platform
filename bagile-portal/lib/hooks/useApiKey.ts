"use client";

import { useEffect, useState } from "react";

export function useApiKey() {
  const [apiKey, setApiKey] = useState<string>("");

  useEffect(() => {
    const key = process.env.NEXT_PUBLIC_BAGILE_API_KEY
      || localStorage.getItem("bagile_api_key")
      || "";
    setApiKey(key);
  }, []);

  return apiKey;
}
