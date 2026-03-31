"use client";

import { Menu, LogOut } from "lucide-react";

interface HeaderProps {
  onMenuClick: () => void;
  breadcrumbs?: { label: string; href?: string }[];
}

export function Header({ onMenuClick, breadcrumbs = [] }: HeaderProps) {
  function handleSignOut() {
    localStorage.removeItem("bagile_api_key");
    localStorage.removeItem("bagile_portal_token");
    localStorage.removeItem("bagile_portal_user");
    window.location.href = "/login";
  }

  return (
    <header className="h-14 bg-white border-b border-gray-200 flex items-center justify-between px-4 lg:px-6">
      <div className="flex items-center gap-3">
        <button onClick={onMenuClick} className="lg:hidden text-gray-500 hover:text-gray-700">
          <Menu className="w-5 h-5" />
        </button>

        {/* Breadcrumbs */}
        <nav className="hidden sm:flex items-center gap-1.5 text-sm">
          {breadcrumbs.map((crumb, i) => (
            <span key={i} className="flex items-center gap-1.5">
              {i > 0 && <span className="text-gray-300">/</span>}
              {crumb.href ? (
                <a href={crumb.href} className="text-gray-500 hover:text-gray-700">{crumb.label}</a>
              ) : (
                <span className="text-gray-900 font-medium">{crumb.label}</span>
              )}
            </span>
          ))}
        </nav>
      </div>

      <button
        onClick={handleSignOut}
        className="flex items-center gap-2 text-sm text-gray-500 hover:text-gray-700"
      >
        <LogOut className="w-4 h-4" />
        <span className="hidden sm:inline">Sign out</span>
      </button>
    </header>
  );
}
