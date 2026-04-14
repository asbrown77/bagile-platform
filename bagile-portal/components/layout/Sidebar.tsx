"use client";

import { usePathname } from "next/navigation";
import Link from "next/link";
import {
  LayoutDashboard, GraduationCap, ArrowLeftRight,
  TrendingUp, Building2, Users, Handshake, Key, X, BarChart3, CalendarDays
} from "lucide-react";

const sections = [
  {
    label: "OPERATE",
    items: [
      { label: "Dashboard", href: "/dashboard", icon: LayoutDashboard },
      { label: "Calendar", href: "/calendar", icon: CalendarDays },
      { label: "Courses", href: "/courses", icon: GraduationCap },
      { label: "Transfers", href: "/transfers", icon: ArrowLeftRight },
    ],
  },
  {
    label: "ANALYSE",
    items: [
      { label: "Revenue", href: "/revenue", icon: TrendingUp },
      { label: "Organisations", href: "/organisations", icon: Building2 },
      { label: "Students", href: "/students", icon: Users },
      { label: "Partners", href: "/partners", icon: Handshake },
      { label: "Course Demand", href: "/demand", icon: BarChart3 },
    ],
  },
  {
    label: "SETTINGS",
    items: [
      { label: "Settings", href: "/settings", icon: Key },
    ],
  },
];

interface SidebarProps {
  pendingTransfers?: number;
  atRiskCourses?: number;
  onClose?: () => void;
}

export function Sidebar({ pendingTransfers = 0, atRiskCourses = 0, onClose }: SidebarProps) {
  const pathname = usePathname();

  const badges: Record<string, number> = {};
  if (pendingTransfers > 0) badges["/transfers"] = pendingTransfers;
  if (atRiskCourses > 0) badges["/courses"] = atRiskCourses;

  return (
    <aside className="flex flex-col w-60 bg-sidebar text-gray-300 h-full">
      {/* Logo */}
      <div className="flex items-center justify-between h-14 px-5 border-b border-gray-800">
        <Link href="/dashboard" className="flex items-center gap-2">
          <img src="https://www.bagile.co.uk/wp-content/uploads/2023/07/bagile-logo-white-01.svg" alt="BAgile" className="h-7" />
        </Link>
        {onClose && (
          <button onClick={onClose} className="text-gray-400 hover:text-white lg:hidden">
            <X className="w-5 h-5" />
          </button>
        )}
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto px-3 py-4">
        {sections.map((section) => (
          <div key={section.label} className="mb-6">
            <p className="px-3 mb-2 text-[10px] font-semibold uppercase tracking-wider text-gray-500">
              {section.label}
            </p>
            {section.items.map((item) => {
              const isActive = pathname === item.href || pathname.startsWith(item.href + "/");
              const Icon = item.icon;
              const badge = badges[item.href];
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  onClick={onClose}
                  className={`flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors mb-0.5
                    ${isActive
                      ? "bg-sidebar-active text-white border-l-2 border-accent -ml-0.5 pl-[10px]"
                      : "text-gray-400 hover:bg-sidebar-hover hover:text-gray-200"
                    }`}
                >
                  <Icon className="w-4 h-4 shrink-0" />
                  <span className="flex-1">{item.label}</span>
                  {badge && (
                    <span className="bg-red-500 text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full min-w-[18px] text-center">
                      {badge}
                    </span>
                  )}
                </Link>
              );
            })}
          </div>
        ))}
      </nav>

      {/* Version */}
      <div className="px-5 py-3 border-t border-gray-800">
        <p className="text-[10px] text-gray-600">BAgile Portal {process.env.NEXT_PUBLIC_APP_VERSION}</p>
      </div>
    </aside>
  );
}
