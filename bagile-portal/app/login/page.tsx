"use client";

import { useEffect, useState, useRef } from "react";
import { loginWithGoogle } from "@/lib/api";

export default function Login() {
  const [error, setError] = useState("");
  const [signingIn, setSigningIn] = useState(false);
  const btnRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (localStorage.getItem("bagile_api_key")) {
      window.location.replace("/dashboard");
      return;
    }

    const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
    if (!clientId) {
      setError("Google Client ID not configured");
      return;
    }

    // Use a global callback so it survives React re-renders
    (window as any).__bagileGoogleCallback = async (response: any) => {
      setError("");
      setSigningIn(true);
      try {
        const data = await loginWithGoogle(response.credential);
        localStorage.setItem("bagile_portal_token", data.token);
        localStorage.setItem("bagile_portal_user", JSON.stringify({ email: data.email, name: data.name }));
        if (data.apiKey) {
          localStorage.setItem("bagile_api_key", data.apiKey);
        }
        window.location.replace("/dashboard");
      } catch (err: any) {
        console.error("Login error:", err);
        setError(err.message || "Login failed");
        setSigningIn(false);
      }
    };

    // Load Google script
    let script = document.querySelector('script[src*="accounts.google.com/gsi/client"]') as HTMLScriptElement;
    if (!script) {
      script = document.createElement("script");
      script.src = "https://accounts.google.com/gsi/client";
      document.head.appendChild(script);
    }

    function initButton() {
      const g = (window as any).google;
      if (!g?.accounts?.id || !btnRef.current) return false;

      g.accounts.id.initialize({
        client_id: clientId,
        callback: (window as any).__bagileGoogleCallback,
      });
      btnRef.current.innerHTML = "";
      g.accounts.id.renderButton(btnRef.current, {
        theme: "outline",
        size: "large",
        text: "signin_with",
        width: 300,
      });
      return true;
    }

    if (!initButton()) {
      const interval = setInterval(() => {
        if (initButton()) clearInterval(interval);
      }, 300);
      setTimeout(() => clearInterval(interval), 10000);
    }
  }, []);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="bg-white rounded-lg shadow-md p-8 max-w-md w-full text-center">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">BAgile</h1>
        <p className="text-gray-600 mb-6">Sign in to access the dashboard</p>
        {signingIn ? (
          <p className="text-gray-500">Signing in...</p>
        ) : (
          <div ref={btnRef} className="flex justify-center" />
        )}
        {error && <p className="mt-4 text-red-600 text-sm">{error}</p>}
      </div>
    </div>
  );
}
