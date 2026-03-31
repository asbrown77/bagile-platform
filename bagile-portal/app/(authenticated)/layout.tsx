"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { getMonitoring, getPendingTransfers } from "@/lib/api";
import { useApiKey } from "@/lib/hooks/useApiKey";

export default function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  const apiKey = useApiKey();
  const [pendingTransfers, setPendingTransfers] = useState(0);
  const [atRiskCourses, setAtRiskCourses] = useState(0);

  useEffect(() => {
    if (!apiKey) return;

    // Load badge counts for sidebar
    getPendingTransfers(apiKey)
      .then((t) => setPendingTransfers(Array.isArray(t) ? t.length : 0))
      .catch(() => {});

    getMonitoring(apiKey, 14)
      .then((courses) => {
        const atRisk = courses.filter((c) => {
          const start = new Date(c.startDate); start.setHours(0, 0, 0, 0);
          const now = new Date(); now.setHours(0, 0, 0, 0);
          return start > now && c.currentEnrolmentCount < 3 && c.daysUntilStart <= 7;
        });
        setAtRiskCourses(atRisk.length);
      })
      .catch(() => {});
  }, [apiKey]);

  return (
    <AppShell pendingTransfers={pendingTransfers} atRiskCourses={atRiskCourses}>
      {children}
    </AppShell>
  );
}
