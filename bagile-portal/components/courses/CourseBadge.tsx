import { getCourseColour } from "@/lib/courseColours";

interface Props {
  /** Full course code, e.g. "PSM-270426-AB" or "PSPOAI-090426-CB" */
  courseCode: string;
  /** Size in px — defaults to 48 */
  size?: number;
  /** Text to show inside the badge — defaults to the first segment of the course code */
  label?: string;
}

/**
 * Circular badge showing the course type abbreviation.
 * Colour is derived from getCourseColour() so it matches scrum.org badge colours
 * used elsewhere in the system (calendar tiles, week strip).
 */
export function CourseBadge({ courseCode, size = 48, label }: Props) {
  const colour = getCourseColour(courseCode);
  const text = label ?? courseCode.split("-")[0].toUpperCase();

  // Scale font size proportionally to the badge size
  const fontSize = Math.round(size * 0.28);

  return (
    <div
      style={{
        width: size,
        height: size,
        backgroundColor: colour,
        borderRadius: "50%",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        flexShrink: 0,
      }}
      title={text}
      aria-label={`${text} course`}
    >
      <span
        style={{
          color: "#ffffff",
          fontSize,
          fontWeight: 700,
          lineHeight: 1,
          letterSpacing: "-0.02em",
          userSelect: "none",
        }}
      >
        {text}
      </span>
    </div>
  );
}
