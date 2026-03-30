"use client";

import { useEffect } from "react";

export default function Home() {
  useEffect(() => {
    const apiKey = localStorage.getItem("bagile_api_key");
    if (apiKey) {
      window.location.replace("/dashboard");
    } else {
      window.location.replace("/login");
    }
  }, []);

  return null;
}
