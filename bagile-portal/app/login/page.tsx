"use client";

import { useEffect, useState } from "react";
import { loginWithGoogle } from "@/lib/api";

export default function Login() {
  const [error, setError] = useState("");

  useEffect(() => {
    // Already logged in? Redirect
    if (localStorage.getItem("bagile_api_key")) {
      window.location.href = "/dashboard";
      return;
    }
    if (localStorage.getItem("bagile_portal_token")) {
      window.location.href = "/settings";
      return;
    }

    const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
    if (!clientId) return;

    const initGoogle = () => {
      if (!(window as any).google?.accounts) {
        setTimeout(initGoogle, 100);
        return;
      }
      (window as any).google.accounts.id.initialize({
        client_id: clientId,
        callback: handleGoogleResponse,
      });
      (window as any).google.accounts.id.renderButton(
        document.getElementById("google-btn"),
        { theme: "outline", size: "large", text: "signin_with", width: 300 }
      );
    };
    initGoogle();
  }, []);

  async function handleGoogleResponse(response: { credential: string }) {
    setError("");
    try {
      const data = await loginWithGoogle(response.credential);
      localStorage.setItem("bagile_portal_token", data.token);
      localStorage.setItem("bagile_portal_user", JSON.stringify({ email: data.email, name: data.name }));
      window.location.href = "/settings";
    } catch (err: any) {
      setError(err.message || "Login failed");
    }
  }

  // Make callback available globally
  useEffect(() => {
    (window as any).handleGoogleResponse = handleGoogleResponse;
  }, []);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="bg-white rounded-lg shadow-md p-8 max-w-md w-full text-center">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">BAgile Portal</h1>
        <p className="text-gray-600 mb-6">Sign in to access the dashboard</p>
        <div id="google-btn" className="flex justify-center" />
        {error && <p className="mt-4 text-red-600 text-sm">{error}</p>}
      </div>
    </div>
  );
}
