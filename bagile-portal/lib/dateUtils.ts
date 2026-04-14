/**
 * Shared date utilities used across calendar views.
 * Extracted to avoid duplication between CalendarView, courses/calendar, and dashboard.
 */

/** Format a Date as YYYY-MM-DD using local time (not UTC) to avoid off-by-one
 *  errors in BST/CET where midnight local = prior day in UTC. */
export function toDateStr(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

/** Add N days to a date, returning a new Date. */
export function addDays(d: Date, n: number): Date {
  const r = new Date(d);
  r.setDate(r.getDate() + n);
  return r;
}

/** Returns Monday of the ISO week containing the given date. */
export function getWeekStart(d: Date): Date {
  const day = d.getDay(); // 0 = Sun
  const diff = day === 0 ? -6 : 1 - day;
  const monday = new Date(d);
  monday.setDate(d.getDate() + diff);
  monday.setHours(0, 0, 0, 0);
  return monday;
}

/** Advance a YYYY-MM-DD string by one day. Used by FullCalendar (dayGrid end is exclusive). */
export function addOneDayStr(dateStr: string): string {
  const d = new Date(dateStr);
  d.setDate(d.getDate() + 1);
  return toDateStr(d);
}
