/**
 * Calendar helpers — badge mapping, status colours, gateway config.
 * Sprint 26: Course Calendar v1.
 */

/** Map courseType code to course logo filename in /public/badges/.
 *  All images are the official scrum.org course logos (not assessment/cert badges). */
const BADGE_MAP: Record<string, string> = {
  PSM:    "PSM-course.png",
  PSMA:   "PSMA-course.png",
  PSPO:   "PSPO-course.png",
  PSPOA:  "PSPOA-course.png",
  PSMAI:  "PSMAI-course.png",
  PSPOAI: "PSPOAI-course.png",
  PSK:    "PSK-course.png",
  PALE:   "PALE-course.png",
  PALEBM: "PALEBM-course.png",
  EBM:    "PALEBM-course.png",  // alias for PAL-EBM
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

/** Scrum.org course types that need the scrum.org gateway. */
const SCRUMORG_TYPES = new Set([
  "PSM", "PSPO", "PSK", "PALE", "PALEBM", "EBM", "APSSD", "APS",
  "PSMA", "PSPOA", "PSMAI", "PSPOAI",
]);

/** ICP course types that need ICAgile gateway. */
function isIcpCourseType(courseType: string): boolean {
  return courseType.toUpperCase().startsWith("ICP");
}

/**
 * Returns the applicable gateway types for a course.
 * Private courses have no public gateways (they are pre-confirmed B2B bookings).
 * Public courses: ecommerce always; scrum.org for Scrum.org course types; icagile for ICP.
 */
export function getApplicableGateways(courseType: string, isPrivate: boolean): string[] {
  if (isPrivate) return [];
  const key = courseType.toUpperCase().replace(/[-_\s]/g, "");
  const gateways = ["ecommerce"];
  if (SCRUMORG_TYPES.has(key)) gateways.push("scrumorg");
  if (isIcpCourseType(courseType)) gateways.push("icagile");
  return gateways;
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
    if (KNOWN_COURSE_TYPES.has(parts[i])) return parts[i];
    // Compound type: APS + SD → APSSD
    if (i + 1 < parts.length) {
      const compound = parts[i] + parts[i + 1];
      if (KNOWN_COURSE_TYPES.has(compound)) return compound;
    }
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
