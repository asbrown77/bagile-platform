"use client";

import { useEffect, useRef } from "react";
import { X } from "lucide-react";

interface SlideOverProps {
  open: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  wide?: boolean;
  /** Optional buttons/actions rendered in the header next to the close button. */
  actions?: React.ReactNode;
}

export function SlideOver({ open, onClose, title, subtitle, children, wide, actions }: SlideOverProps) {
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const handleEsc = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    document.addEventListener("keydown", handleEsc);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", handleEsc);
      document.body.style.overflow = "";
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50">
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/40 transition-opacity" onClick={onClose} />

      {/* Panel — full-width on mobile, fixed width on sm+ */}
      <div className="fixed inset-y-0 right-0 w-full sm:w-auto flex">
        <div
          ref={panelRef}
          className={`relative bg-white shadow-xl flex flex-col w-full ${wide ? "sm:w-[600px]" : "sm:w-[480px]"}`}
        >
          {/* Header */}
          <div className="flex items-center justify-between px-4 sm:px-6 py-4 border-b border-gray-200">
            <div>
              <h2 className="text-lg font-semibold text-gray-900">{title}</h2>
              {subtitle && <p className="text-sm text-gray-500 mt-0.5">{subtitle}</p>}
            </div>
            <div className="flex items-center gap-2">
              {actions}
              <button onClick={onClose} className="text-gray-400 hover:text-gray-600 rounded-lg p-1 hover:bg-gray-100">
                <X className="w-5 h-5" />
              </button>
            </div>
          </div>

          {/* Body — scrollable */}
          <div className="flex-1 overflow-y-auto px-4 sm:px-6 py-4">
            {children}
          </div>
        </div>
      </div>
    </div>
  );
}
