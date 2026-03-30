"use client";

import { useEffect } from "react";

export default function Home() {
  useEffect(() => {
    // If user has an API key, go straight to dashboard
    const apiKey = localStorage.getItem("bagile_api_key");
    if (apiKey) {
      window.location.href = "/dashboard";
      return;
    }
    // If user is logged in via portal but has no API key, go to settings to create one
    const token = localStorage.getItem("bagile_portal_token");
    if (token) {
      window.location.href = "/settings";
      return;
    }
    // Otherwise go to login
    window.location.href = "/login";
  }, []);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <p className="text-gray-500">Loading...</p>
    </div>
  );
}
