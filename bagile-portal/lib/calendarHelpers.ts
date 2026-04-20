/**
 * Calendar helpers — badge mapping, status colours, gateway config.
 * Sprint 26: Course Calendar v1.
 */

/** Map courseType code to course logo filename in /public/badges/.
 *  All images are the official scrum.org course logos (not assessment/cert badges). */
const BADGE_MAP: Record<string, string> = {
  PSM:    "PSM-course.png",
  PSMO:   "PSM-course.png",    // WooCommerce alias for PSM
  PSMA:   "PSMA-course.png",
  PSPO:   "PSPO-course.png",
  PSPOA:  "PSPOA-course.png",
  PSMAI:  "PSMAI-course.png",
  PSPOAI: "PSPOAI-course.png",
  PSK:    "PSK-course.png",
  PALE:   "PALE-course.png",
  PAL:    "PALE-course.png",   // WooCommerce alias for PAL-E
  PALEBM: "PALEBM-course.png",
  EBM:    "PALEBM-course.png", // alias for PAL-EBM
  PSU:    "PSU-course.png",
  PSFS:   "PSFS-course.png",
  APSSD:  "APSSD-course.png",
  APS:    "APS-course.png",
  SPS:    "SPS-course.png",
  PSPBM:  "PSPBM-course.png",
  PSMPO:  "PSMPO-course.png",  // combined PSM + PSPO
};

export function getBadgeSrc(courseType: string): string | null {
  const key = courseType.toUpperCase().replace(/[-_\s]/g, "");
  return BADGE_MAP[key] ? `/badges/${BADGE_MAP[key]}` : null;
}

/** Friendly course type display names. */
const COURSE_NAMES: Record<string, string> = {
  PSM:    "Professional Scrum Master",
  PSMA:   "Professional Scrum Master Advanced",
  PSPO:   "Professional Scrum Product Owner",
  PSPOA:  "Professional Scrum Product Owner Advanced",
  PSMAI:  "Professional Scrum Master with AI Essentials",
  PSPOAI: "Professional Scrum Product Owner with AI Essentials",
  PSK:    "Professional Scrum with Kanban",
  PALE:   "Professional Agile Leadership Essentials",
  PALEBM: "Professional Agile Leadership with EBM",
  EBM:    "Professional Agile Leadership with EBM",  // alias for PAL-EBM
  PSU:    "Professional Scrum with User Experience",
  PSFS:   "Professional Scrum Facilitation Skills",
  APSSD:  "Applying Professional Scrum for Software Development",
  APS:    "Applying Professional Scrum",
  PSMPO:  "Professional Scrum Master & Product Owner",
  ICP:    "ICAgile Certified Professional",
  ICPATF: "ICAgile Agile Team Facilitation",
  ICPACC: "ICAgile Agile Coaching Certification",
};

export function getCourseDisplayName(courseType: string): string {
  const key = courseType.toUpperCase().replace(/[-_\s]/g, "");
  return COURSE_NAMES[key] || courseType;
}

/** Human-readable code with hyphens: PSMAI -> PSM-AI, PSPOA -> PSPO-A */
const CODE_DISPLAY: Record<string, string> = {
  PSM:    "PSM",
  PSMA:   "PSM-A",
  PSPO:   "PSPO",
  PSPOA:  "PSPO-A",
  PSMAI:  "PSM-AI",
  PSPOAI: "PSPO-AI",
  PSK:    "PSK",
  PALE:   "PAL-E",
  PALEBM: "PAL-EBM",
  EBM:    "EBM",       // alias — displays as EBM per convention
  PSU:    "PSU",
  PSFS:   "PSFS",
  APSSD:  "APS-SD",
  APS:    "APS",
  PSMPO:  "PSM+PO",
  ICP:    "ICP",
  ICPATF: "ICP-ATF",
  ICPACC: "ICP-ACC",
};

export function getCourseCodeDisplay(courseType: string): string {
  const key = courseType.toUpperCase().replace(/[-_\s]/g, "");
  return CODE_DISPLAY[key] || courseType;
}

/** Calendar status left-border colours. */
export const STATUS_COLOURS: Record<string, string> = {
  planned: "#9ca3af",
  enquiry: "#a78bfa",    // purple — early stage, no commitment
  quoted: "#60a5fa",     // blue — quote sent
  confirmed: "#22c55e",  // green — booked
  completed: "#6b7280",  // grey — done
  partial_live: "#f59e0b",
  live: "#22c55e",
  cancelled: "#ef4444",
};

export function getStatusColour(status: string): string {
  return STATUS_COLOURS[status] || STATUS_COLOURS.planned;
}

/** Status badge variant mapping for the Badge component. */
export function getStatusBadgeVariant(status: string): "neutral" | "warning" | "success" | "danger" {
  switch (status) {
    case "planned":      return "neutral";
    case "enquiry":      return "neutral";
    case "quoted":       return "neutral";
    case "confirmed":    return "success";
    case "completed":    return "neutral";
    case "partial_live": return "warning";
    case "live":         return "success";
    case "cancelled":    return "danger";
    default:             return "neutral";
  }
}

export function getStatusLabel(status: string): string {
  switch (status) {
    case "planned":      return "Planned";
    case "enquiry":      return "Enquiry";
    case "quoted":       return "Quoted";
    case "confirmed":    return "Confirmed";
    case "completed":    return "Completed";
    case "partial_live": return "Partial Live";
    case "live":         return "Live";
    case "cancelled":    return "Cancelled";
    default:             return status;
  }
}

/** Whether a decision deadline is urgent (within 5 days of today). */
export function isDeadlineUrgent(deadline: string | null): boolean {
  if (!deadline) return false;
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  const dl = new Date(deadline);
  dl.setHours(0, 0, 0, 0);
  const diff = (dl.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);
  return diff >= 0 && diff <= 5;
}

// ── UK Bank Holidays (England) — update annually ─────────────
// Source: https://www.gov.uk/bank-holidays
const UK_BANK_HOLIDAYS = new Set([
  // 2025
  "2025-01-01", "2025-04-18", "2025-04-21", "2025-05-05",
  "2025-05-26", "2025-08-25", "2025-12-25", "2025-12-26",
  // 2026
  "2026-01-01", "2026-04-03", "2026-04-06", "2026-05-04",
  "2026-05-25", "2026-08-31", "2026-12-25", "2026-12-28",
]);

/** Count working days (Mon–Fri, excluding UK bank holidays) from today up to (not including) a target date. */
export function workingDaysUntil(targetDate: string): number {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const target = new Date(targetDate);
  target.setHours(0, 0, 0, 0);

  let count = 0;
  const cursor = new Date(today);
  while (cursor < target) {
    const day = cursor.getDay(); // 0=Sun, 6=Sat
    const iso = cursor.toISOString().slice(0, 10);
    if (day !== 0 && day !== 6 && !UK_BANK_HOLIDAYS.has(iso)) count++;
    cursor.setDate(cursor.getDate() + 1);
  }
  return count;
}

/**
 * Whether a public course is below minimum enrolment and close enough to
 * warrant a warning. Thresholds come from the portal config (Settings →
 * Courses → Course Risk Thresholds):
 *  - workingDays: interpreted as working days until start (UK bank holidays excluded)
 *  - minEnrolments: warn when enrolmentCount is below this
 */
export function isLowEnrolment(
  event: {
    isPrivate: boolean;
    status: string;
    enrolmentCount: number;
    minimumEnrolments: number;
    startDate: string;
  },
  thresholds: { workingDays: number; minEnrolments: number }
): boolean {
  if (event.isPrivate) return false;
  if (event.status === "cancelled" || event.status === "planned") return false;
  // Use whichever minimum is more conservative
  const minCount = Math.max(event.minimumEnrolments, thresholds.minEnrolments);
  if (event.enrolmentCount >= minCount) return false;
  return workingDaysUntil(event.startDate) <= thresholds.workingDays;
}

/** Normalise a course type code: uppercase, strip hyphens/underscores/spaces. */
function normaliseCourseType(code: string): string {
  return code.toUpperCase().replace(/[-_\s]/g, "");
}

/**
 * WooCommerce SKU shorthands → normalised canonical course_definitions code.
 * These mirror the entries in BADGE_MAP / COURSE_NAMES so alias resolution stays
 * in sync without extra API calls.
 */
const WOOCOMMERCE_ALIAS_MAP: Record<string, string> = {
  EBM:  "PALEBM", // PAL-EBM on WooCommerce is listed as EBM
  PAL:  "PALE",   // PAL-E shorthand
  PSMO: "PSM",    // occasional WooCommerce PSM variant
};

/**
 * Look up the provider for a courseType from the course definitions list.
 * Tries: 1) direct normalised code match, 2) static WooCommerce alias map,
 * 3) aliases array on each CourseDef (populated in CourseDefsEditor context).
 * Returns null if courseDefs not supplied or no match found.
 */
function resolveProvider(
  courseType: string,
  courseDefs?: { code: string; provider?: string | null; aliases?: string[] }[]
): string | null {
  if (!courseDefs || courseDefs.length === 0) return null;
  const key = normaliseCourseType(courseType);
  // 1. Direct match
  const direct = courseDefs.find((d) => normaliseCourseType(d.code) === key);
  if (direct) return direct.provider ?? null;
  // 2. Static WooCommerce alias map
  const canonical = WOOCOMMERCE_ALIAS_MAP[key];
  if (canonical) {
    const aliased = courseDefs.find((d) => normaliseCourseType(d.code) === canonical);
    if (aliased) return aliased.provider ?? null;
  }
  // 3. aliases[] on each def (present when loaded via CourseDefsEditor)
  const byAlias = courseDefs.find((d) =>
    d.aliases?.some((a) => normaliseCourseType(a) === key)
  );
  return byAlias?.provider ?? null;
}

/**
 * Returns the applicable gateway types for a course.
 * Private courses have no public gateways (they are pre-confirmed B2B bookings).
 * Public courses: ecommerce always; scrumorg/icagile when provider matches.
 * Pass courseDefs to use DB-stored provider; falls back to ecommerce-only when omitted.
 */
export function getApplicableGateways(
  courseType: string,
  isPrivate: boolean,
  courseDefs?: { code: string; provider?: string | null }[]
): string[] {
  if (isPrivate) return [];
  const gateways = ["ecommerce"];
  const provider = resolveProvider(courseType, courseDefs);
  if (provider === "scrumorg") gateways.push("scrumorg");
  if (provider === "icagile") gateways.push("icagile");
  return gateways;
}

/**
 * Returns "ecommerce" for public courses, null for private.
 * Private courses are pre-confirmed B2B bookings — no shop product needed.
 */
export function getShopGateway(isPrivate: boolean): string | null {
  return isPrivate ? null : "ecommerce";
}

/**
 * Returns the external listing gateway for a course ("scrumorg" | "icagile" | null).
 * Pass courseDefs to use DB-stored provider. Returns null when courseDefs not supplied
 * or the course has no external gateway.
 */
export function getExternalGateway(
  courseType: string,
  isPrivate: boolean,
  courseDefs?: { code: string; provider?: string | null }[]
): string | null {
  if (isPrivate) return null;
  const provider = resolveProvider(courseType, courseDefs);
  if (provider === "scrumorg") return "scrumorg";
  if (provider === "icagile") return "icagile";
  return null;
}

/** Human-readable label for a gateway type. */
export function getGatewayLabel(gwType: string): string {
  switch (gwType) {
    case "ecommerce": return "Shop";
    case "scrumorg":  return "Scrum.org";
    case "icagile":   return "IC Agile";
    default:          return gwType;
  }
}

// All recognised course type codes — used to find the type within a SKU
const KNOWN_COURSE_TYPES = new Set([
  "PSM", "PSMO", "PSMAI", "PSMA", "PSPO", "PSPOAI", "PSPOA",
  "PSK", "PALE", "PAL", "PSU", "PSFS", "EBM", "PALEBM", "APS", "APSSD",
  "PSMPO", "ICP", "ICPATF", "ICPACC",
]);

/**
 * Extract a clean course type code from a course SKU.
 * Handles old format (TYPE-DATE-ORG-PRIV-DATE) and new format (ORG-TYPE-DATE).
 * e.g. "APSSD-050526-DVSA-PRIV-050526" → "APSSD"
 *      "PSM-270426-FNC-PRIV-270426"    → "PSM"
 *      "FNC-PSM-270426"                → "PSM"  (new format)
 *      "DVSA-APSSD-050526"             → "APSSD"
 */
export function extractCourseTypeFromSku(sku: string): string {
  const parts = sku.toUpperCase().split("-");
  for (let i = 0; i < parts.length; i++) {
    if (/^\d{6}$/.test(parts[i]) || parts[i] === "PRIV") break;
    // Check compound FIRST so "APS-SD-..." → APSSD, not APS
    if (i + 1 < parts.length) {
      const compound = parts[i] + parts[i + 1];
      if (KNOWN_COURSE_TYPES.has(compound)) return compound;
    }
    if (KNOWN_COURSE_TYPES.has(parts[i])) return parts[i];
  }
  // Fallback: first non-date, non-PRIV segment
  return parts.find((p) => !/^\d{6}$/.test(p) && p !== "PRIV") || sku.toUpperCase();
}

/** All course type options for the Add Course form. */
export const COURSE_TYPE_OPTIONS = [
  { value: "PSM", label: "PSM" },
  { value: "PSPO", label: "PSPO" },
  { value: "PSMA", label: "PSM-A" },
  { value: "PSPOA", label: "PSPO-A" },
  { value: "PSMAI", label: "PSM-AI" },
  { value: "PSPOAI", label: "PSPO-AI" },
  { value: "PSK", label: "PSK" },
  { value: "PALE", label: "PAL-E" },
  { value: "PALEBM", label: "EBM" },
  { value: "APS", label: "APS" },
  { value: "APSSD", label: "APS-SD" },
  { value: "PSFS", label: "PSFS" },
  { value: "PSU", label: "PSU" },
  { value: "ICP", label: "ICP" },
  { value: "ICPATF", label: "ICP-ATF" },
  { value: "ICPACC", label: "ICP-ACC" },
];
