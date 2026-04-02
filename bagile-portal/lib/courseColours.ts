/**
 * Course type colour map — matches scrum.org badge colours.
 * Used for calendar tiles, week strip pills, and any future course visualisations.
 *
 * Matching is prefix-based: the first segment of a course code (before the first "-")
 * is looked up against this map. Unknown types fall back to FALLBACK_COLOUR.
 */

/** Scrum.org badge colours by course type prefix. */
const COURSE_COLOUR_MAP: Record<string, string> = {
  PSM:  "#3C7F9C", // Steel blue  — covers PSM, PSMAI, PSMA
  PSPO: "#82A53F", // Olive green — covers PSPO, PSPOAI, PSPOA
  PSK:  "#660C3B", // Burgundy
  PALE: "#0C7152", // Dark teal (PAL-E first segment parses as "PALE")
  EBM:  "#0C7152", // Dark teal  — same family as PAL-E
  PSFS: "#F7921D", // Bright orange
  APS:  "#D04628", // Burnt red  — APS-SD
  PSU:  "#E6196C", // Hot pink
};

export const FALLBACK_COLOUR = "#6B7280"; // Tailwind gray-500

/**
 * Returns the hex colour for a given course code.
 * Matches by stripping hyphens from the first segment for normalisation,
 * then doing a longest-prefix match against the map.
 *
 * Examples:
 *   "PSMAI-090426-CB"  → PSM prefix → #3C7F9C
 *   "PSPOAI-090426-AB" → PSPO prefix → #82A53F
 *   "APS-SD-090426-CB" → APS prefix → #D04628
 */
export function getCourseColour(courseCode: string): string {
  // The first segment before "-" is the type key (already normalised in most codes).
  // For codes like "APS-SD-ddmmyy-TR", the first segment is "APS".
  const firstSegment = courseCode.split("-")[0].toUpperCase();

  // Try longest-prefix match (e.g. PSPOAI should match PSPO before PSP)
  const keys = Object.keys(COURSE_COLOUR_MAP).sort((a, b) => b.length - a.length);
  for (const key of keys) {
    if (firstSegment.startsWith(key)) {
      return COURSE_COLOUR_MAP[key];
    }
  }

  return FALLBACK_COLOUR;
}

/** Trainer avatar colours — consistent across all views. */
export const TRAINER_COLOURS: Record<string, string> = {
  AB: "#E8792B", // BAgile orange (Alex Brown)
  CB: "#6366F1", // Indigo (Chris Bexon)
};

export const FALLBACK_TRAINER_COLOUR = "#6B7280";

/**
 * Returns initials from a trainer name string, max 2 characters.
 * "Alex Brown" → "AB", "Chris Bexon" → "CB", "Alex" → "AL"
 */
export function trainerInitials(trainerName: string | null | undefined): string {
  if (!trainerName) return "?";
  const parts = trainerName.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

/** Returns the avatar background colour for a set of trainer initials. */
export function getTrainerColour(initials: string): string {
  return TRAINER_COLOURS[initials] ?? FALLBACK_TRAINER_COLOUR;
}
