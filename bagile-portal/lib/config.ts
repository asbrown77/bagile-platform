/**
 * Portal configuration — localStorage-backed settings with sensible defaults.
 * Used for thresholds and preferences that the user may want to tune.
 */

const STORAGE_KEY = "bagile_portal_config";

export interface PortalConfig {
  /** Days until course start to flag as "at risk". Default: 2 */
  atRiskDays: number;
  /** Minimum enrolments to consider a course safe. Default: 3 */
  minEnrolments: number;
}

const DEFAULTS: PortalConfig = {
  atRiskDays: 2,
  minEnrolments: 3,
};

export function loadConfig(): PortalConfig {
  if (typeof window === "undefined") return DEFAULTS;
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return DEFAULTS;
    const parsed = JSON.parse(raw);
    return { ...DEFAULTS, ...parsed };
  } catch {
    return DEFAULTS;
  }
}

export function saveConfig(config: Partial<PortalConfig>): PortalConfig {
  const current = loadConfig();
  const updated = { ...current, ...config };
  localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
  return updated;
}
