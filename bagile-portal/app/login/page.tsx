"use client";

import { useEffect, useState } from "react";
import { loginWithGoogle } from "@/lib/api";

export default function Login() {
  const [error, setError] = useState("");
  const [signingIn, setSigningIn] = useState(false);

  useEffect(() => {
    // Already have a key? Go to dashboard
    if (localStorage.getItem("bagile_api_key")) {
      window.location.replace("/dashboard");
      return;
    }

    const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
    if (!clientId) return;

    const initGoogle = () => {
      const g = (window as any).google;
      if (!g?.accounts) {
        setTimeout(initGoogle, 200);
        return;
      }
      g.accounts.id.initialize({
        client_id: clientId,
        callback: async (response: { credential: string }) => {
          setError("");
          setSigningIn(true);
          try {
            const data = await loginWithGoogle(response.credential);
            localStorage.setItem("bagile_portal_token", data.token);
            localStorage.setItem("bagile_portal_user", JSON.stringify({ email: data.email, name: data.name }));
            // API key auto-created on first login
            if (data.apiKey) {
              localStorage.setItem("bagile_api_key", data.apiKey);
            }
            window.location.replace("/dashboard");
          } catch (err: any) {
            setError(err.message || "Login failed");
            setSigningIn(false);
          }
        },
      });
      g.accounts.id.renderButton(
        document.getElementById("google-btn"),
        { theme: "outline", size: "large", text: "signin_with", width: 300 }
      );
    };
    initGoogle();
  }, []);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="bg-white rounded-lg shadow-md p-8 max-w-md w-full text-center">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">BAgile</h1>
        <p className="text-gray-600 mb-6">Sign in to access the dashboard</p>
        {signingIn ? (
          <p className="text-gray-500">Signing in...</p>
        ) : (
          <div id="google-btn" className="flex justify-center" />
        )}
        {error && <p className="mt-4 text-red-600 text-sm">{error}</p>}
      </div>
    </div>
  );
}
