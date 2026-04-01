"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { getMonitoring, getPendingTransfers } from "@/lib/api";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { getCourseDisplayStatus } from "@/lib/courseStatus";

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
        const flagged = courses.filter((c) => {
          const status = getCourseDisplayStatus(c);
          return status === "at risk" || status === "cancel";
        });
        setAtRiskCourses(flagged.length);
      })
      .catch(() => {});
  }, [apiKey]);

  return (
    <AppShell pendingTransfers={pendingTransfers} atRiskCourses={atRiskCourses}>
      {children}
    </AppShell>
  );
}
