/**
 * Shared course display status logic.
 * Single source of truth — used by courses page, calendar, dashboard, and layout.
 */

import { loadConfig } from "./config";

interface CourseForStatus {
  startDate: string | null;
  endDate?: string | null;
  currentEnrolmentCount: number;
  needsAttention?: boolean;
  guaranteedToRun?: boolean;
  status?: string | null;
  daysUntilStart?: number;
}

export type CourseDisplayStatus =
  | "running"
  | "completed"
  | "cancel"
  | "at risk"
  | "guaranteed"
  | "monitor"
  | "cancelled";

export function getCourseDisplayStatus(course: CourseForStatus): CourseDisplayStatus {
  if (course.status === "cancelled") return "cancelled";

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const start = new Date(course.startDate || "");
  start.setHours(0, 0, 0, 0);

  const end = course.endDate ? new Date(course.endDate) : start;
  end.setHours(0, 0, 0, 0);

  if (start <= today && today <= end) return "running";
  if (today > end) return "completed";

  const { atRiskDays, minEnrolments } = loadConfig();
  const daysAway = course.daysUntilStart ?? Math.round((start.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
  const lowEnrolment = course.currentEnrolmentCount < minEnrolments;

  if (lowEnrolment && daysAway <= 0) return "cancel";
  if (lowEnrolment && daysAway <= atRiskDays) return "at risk";

  return course.currentEnrolmentCount >= minEnrolments ? "guaranteed" : "monitor";
}
