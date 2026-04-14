"use client";

import { useEffect, useState, useCallback } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { CourseScheduleItem, getCourseSchedules } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { Button } from "@/components/ui/Button";
import { List } from "lucide-react";
import Link from "next/link";
import { CalendarView } from "@/components/courses/CalendarView";

export default function CoursesCalendarPage() {
  const apiKey = useApiKey();
  const [courses, setCourses] = useState<CourseScheduleItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [range, setRange] = useState<{ from: string; to: string } | null>(null);

  const loadCourses = useCallback(async () => {
    if (!apiKey || !range) return;
    setLoading(true);
    setError("");
    try {
      const result = await getCourseSchedules(apiKey, { from: range.from, to: range.to, pageSize: 100 });
      setCourses((result.items || []).filter((c) => c.title && c.startDate));
    } catch {
      setError("Failed to load courses");
    } finally {
      setLoading(false);
    }
  }, [apiKey, range]);

  useEffect(() => { loadCourses(); }, [loadCourses]);

  return (
    <>
      <PageHeader
        title="Course Calendar"
        actions={
          <Link href="/courses">
            <Button variant="secondary" size="sm"><List className="w-3.5 h-3.5" /> List View</Button>
          </Link>
        }
      />
      {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}
      <CalendarView
        courses={courses}
        loading={loading}
        storageKey="bagile_cal_view"
        onRangeChange={(from, to) => setRange({ from, to })}
      />
    </>
  );
}
