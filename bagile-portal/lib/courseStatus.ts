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
  /** CourseScheduleItem uses `status`; MonitoringCourse sends `courseStatus` from the API.
   *  Both are checked so this function works correctly for both shapes. */
  status?: string | null;
  courseStatus?: string | null;
  daysUntilStart?: number;
  /** Private courses are always confirmed — enrolment-based risk logic does not apply. */
  type?: string | null;
}

export type CourseDisplayStatus =
  | "running"
  | "completed"
  | "cancel"
  | "at risk"
  | "guaranteed"
  | "confirmed"
  | "monitor"
  | "cancelled";

export function getCourseDisplayStatus(course: CourseForStatus): CourseDisplayStatus {
  // MonitoringCourse sends the lifecycle status as `courseStatus`; CourseScheduleItem
  // sends it as `status`. Check both so this works for either shape.
  if (course.status === "cancelled" || course.courseStatus === "cancelled") return "cancelled";

  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const start = new Date(course.startDate || "");
  start.setHours(0, 0, 0, 0);

  const end = course.endDate ? new Date(course.endDate) : start;
  end.setHours(0, 0, 0, 0);

  if (start <= today && today <= end) return "running";
  if (today > end) return "completed";

  // Private courses: enrolment-based risk logic is irrelevant (client decides
  // how many attend). "guaranteed" implies minimum enrolment met, which doesn't
  // apply. Always show "confirmed" for private upcoming courses.
  if (course.type === "private") return "confirmed";

  const { atRiskDays, minEnrolments } = loadConfig();
  const daysAway = course.daysUntilStart ?? Math.round((start.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
  const lowEnrolment = course.currentEnrolmentCount < minEnrolments;

  if (lowEnrolment && daysAway <= 0) return "cancel";
  if (lowEnrolment && daysAway <= atRiskDays) return "at risk";

  return course.currentEnrolmentCount >= minEnrolments ? "guaranteed" : "monitor";
}
