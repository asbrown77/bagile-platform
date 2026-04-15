/**
 * Shared helpers for auto-generating private course name and invoice reference.
 * Used by CreatePrivateCoursePanel and EditPrivateCoursePanel.
 */

const COURSE_FULL_NAMES: Record<string, string> = {
  PSM:    "Professional Scrum Master",
  PSMO:   "Professional Scrum Master",       // alias
  PSMAI:  "Professional Scrum Master (AI Edition)",
  PSMA:   "Professional Scrum Master Advanced",
  PSPO:   "Professional Scrum Product Owner",
  PSPOAI: "Professional Scrum Product Owner (AI Edition)",
  PSPOA:  "Professional Scrum Product Owner Advanced",
  PSK:    "Professional Scrum with Kanban",
  PALE:   "Professional Agile Leadership Essentials",
  PAL:    "Professional Agile Leadership",
  PSU:    "Professional Scrum with User Experience",
  PSFS:   "Professional Scrum Facilitation Skills",
  PALEBM: "Professional Agile Leadership with EBM",
  EBM:    "Professional Agile Leadership with EBM",  // alias for PAL-EBM
  APS:    "Applying Professional Scrum",
  APSSD:  "Applying Professional Scrum for Software Development",
  PSMPO:  "Professional Scrum Master & Product Owner",
  ICP:    "ICAgile Certified Professional",
  ICPATF: "ICAgile Agile Team Facilitation",
  ICPACC: "ICAgile Agile Coaching Certification",
};

const FORMAT_LABELS: Record<string, string> = {
  virtual:  "Virtual",
  in_person: "In-person",
};

/**
 * Generate a course name from its components.
 * e.g. "Professional Scrum Master - Frazer-Nash Consultancy Ltd (In-person)"
 */
export function generateCourseName(
  courseCode: string,
  orgName: string,
  formatType: string,
): string {
  const fullName = COURSE_FULL_NAMES[courseCode.toUpperCase()] ?? courseCode;
  const format = FORMAT_LABELS[formatType] ?? formatType;
  if (!orgName) return `${fullName} (${format})`;
  return `${fullName} - ${orgName} (${format})`;
}

/**
 * Generate an invoice reference (also used as the course SKU) from its components.
 * Format: {OrgAcronym}-{CourseCode}-{DDMMYY}
 * e.g. "FNC-PSM-270426" — matches Xero invoice naming convention
 */
export function generateInvoiceRef(
  courseCode: string,
  orgAcronym: string,
  startDate: string,   // ISO date string e.g. "2026-04-27"
): string {
  if (!startDate) return "";
  const d = new Date(startDate);
  if (isNaN(d.getTime())) return "";
  const dd = String(d.getDate()).padStart(2, "0");
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const yy = String(d.getFullYear()).slice(2);
  const datePart = `${dd}${mm}${yy}`;
  if (!orgAcronym) return `${courseCode.toUpperCase()}-${datePart}`;
  return `${orgAcronym.toUpperCase()}-${courseCode.toUpperCase()}-${datePart}`;
}
