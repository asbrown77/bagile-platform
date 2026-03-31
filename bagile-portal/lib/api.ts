const API_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.bagile.co.uk";

// ── Shared request helper ────────────────────────────────

async function apiRequest<T>(path: string, apiKey: string, options?: { method?: string; body?: unknown }): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    method: options?.method || "GET",
    headers: {
      "X-Api-Key": apiKey,
      ...(options?.body ? { "Content-Type": "application/json" } : {}),
    },
    ...(options?.body ? { body: JSON.stringify(options.body) } : {}),
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}

// ── Portal Auth (JWT) ────────────────────────────────────

export interface ApiKey {
  id: string;
  keyprefix: string;
  label: string;
  owneremail: string;
  ownername: string;
  isactive: boolean;
  createdat: string;
  lastusedat: string | null;
  revokedat: string | null;
}

export interface CreateKeyResponse {
  id: string;
  key: string;
  prefix: string;
  label: string;
  message: string;
}

export async function loginWithGoogle(idToken: string) {
  const res = await fetch(`${API_URL}/portal/auth/google`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ idToken }),
  });
  if (!res.ok) throw new Error("Login failed");
  return res.json() as Promise<{ token: string; email: string; name: string; apiKey?: string }>;
}

export async function listKeys(token: string): Promise<ApiKey[]> {
  const res = await fetch(`${API_URL}/portal/keys`, { headers: { Authorization: `Bearer ${token}` } });
  if (!res.ok) throw new Error("Failed to list keys");
  return res.json();
}

export async function createKey(token: string, label: string): Promise<CreateKeyResponse> {
  const res = await fetch(`${API_URL}/portal/keys`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" },
    body: JSON.stringify({ label }),
  });
  if (!res.ok) throw new Error("Failed to create key");
  return res.json();
}

export async function revokeKey(token: string, id: string): Promise<void> {
  const res = await fetch(`${API_URL}/portal/keys/${id}`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to revoke key");
}

// ── Course Monitoring ────────────────────────────────────

export interface MonitoringCourse {
  id: number;
  title: string;
  courseCode: string;
  startDate: string;
  endDate: string | null;
  trainerName: string | null;
  location: string | null;
  currentEnrolmentCount: number;
  minimumRequired: number;
  fillPercentage: number;
  monitoringStatus: string;
  daysUntilStart: number;
  daysUntilDecision: number;
  decisionDeadline: string | null;
  recommendedAction: string;
}

export async function getMonitoring(apiKey: string, daysAhead = 60): Promise<MonitoringCourse[]> {
  return apiRequest(`/api/course-schedules/monitoring?daysAhead=${daysAhead}`, apiKey);
}

// ── Course Attendees ─────────────────────────────────────

export interface CourseAttendee {
  enrolmentId: number;
  studentId: number;
  firstName: string;
  lastName: string;
  name: string;
  email: string;
  organisation: string | null;
  status: string;
  courseCode: string | null;
  courseName: string | null;
  country: string | null;
  orderNumber: string | null;
  orderAmount: number | null;
  orderStatus: string | null;
  currency: string | null;
  billingCompany: string | null;
  billingName: string | null;
  billingEmail: string | null;
  paymentMethod: string | null;
  orderAttendeeCount: number | null;
}

export async function getCourseAttendees(apiKey: string, courseId: number): Promise<CourseAttendee[]> {
  return apiRequest(`/api/course-schedules/${courseId}/attendees`, apiKey);
}

// ── Revenue ──────────────────────────────────────────────

export interface RevenueSummary {
  currentMonthRevenue: number;
  currentYearRevenue: number;
  previousYearRevenue: number;
  currentMonthOrders: number;
  currentYearOrders: number;
  monthlyBreakdown: MonthlyRevenue[];
  byCourseType: CourseTypeRevenue[];
  previousYearMonthly: MonthlyRevenue[];
  bySource: SourceRevenue[];
}

export interface MonthlyRevenue {
  year: number;
  month: number;
  monthName: string;
  revenue: number;
  orderCount: number;
  attendeeCount: number;
}

export interface CourseTypeRevenue {
  courseType: string;
  revenue: number;
  orderCount: number;
  attendeeCount: number;
}

export interface SourceRevenue {
  source: string;
  revenue: number;
  orderCount: number;
  attendeeCount: number;
}

export interface MonthDrilldown {
  year: number;
  month: number;
  monthName: string;
  totalRevenue: number;
  totalOrders: number;
  orders: MonthDrilldownOrder[];
}

export interface MonthDrilldownOrder {
  orderId: number;
  externalId: string;
  orderDate: string | null;
  company: string | null;
  contactName: string | null;
  contactEmail: string | null;
  netRevenue: number;
  grossRevenue: number;
  refundAmount: number;
  lifecycleStatus: string | null;
  paymentMethod: string | null;
  courseCode: string | null;
  courseName: string | null;
  courseDate: string | null;
  courseType: string | null;
  attendeeCount: number;
}

export async function getRevenueSummary(apiKey: string, year?: number): Promise<RevenueSummary> {
  const qs = year ? `?year=${year}` : "";
  return apiRequest(`/api/analytics/revenue${qs}`, apiKey);
}

export async function getRevenueMonthDrilldown(apiKey: string, year: number, month: number): Promise<MonthDrilldown> {
  return apiRequest(`/api/analytics/revenue/${year}/${month}`, apiKey);
}

// ── Organisation Analytics ───────────────────────────────

export interface OrganisationAnalytics {
  company: string;
  partnerType: string | null;
  ptnTier: string | null;
  discountRate: number | null;
  orderCount: number;
  delegateCount: number;
  totalSpend: number;
}

export interface RepeatCustomer {
  company: string;
  totalBookings: number;
  lifetimeSpend: number;
  lifetimeDelegates: number;
  firstBooking: string | null;
  lastBooking: string | null;
  relationshipDays: number;
  bookingsThisYear: number;
  spendThisYear: number;
}

export async function getOrganisationAnalytics(apiKey: string, year?: number, sortBy?: string): Promise<{ year: number; organisations: OrganisationAnalytics[] }> {
  const params = new URLSearchParams();
  if (year) params.set("year", String(year));
  if (sortBy) params.set("sortBy", sortBy);
  const qs = params.toString() ? `?${params}` : "";
  return apiRequest(`/api/analytics/organisations${qs}`, apiKey);
}

export async function getRepeatCustomers(apiKey: string, year?: number, minBookings?: number): Promise<RepeatCustomer[]> {
  const params = new URLSearchParams();
  if (year) params.set("year", String(year));
  if (minBookings) params.set("minBookings", String(minBookings));
  const qs = params.toString() ? `?${params}` : "";
  return apiRequest(`/api/analytics/organisations/repeat-customers${qs}`, apiKey);
}

// ── Partner Analytics ────────────────────────────────────

export interface PartnerAnalytics {
  name: string;
  ptnTier: string | null;
  discountRate: number | null;
  contactEmail: string | null;
  bookingsThisYear: number;
  delegatesThisYear: number;
  spendThisYear: number;
  calculatedTier: string;
  calculatedDiscount: number;
  tierMismatch: boolean;
}

export async function getPartnerAnalytics(apiKey: string): Promise<PartnerAnalytics[]> {
  return apiRequest("/api/analytics/partners", apiKey);
}

// ── Course Demand ────────────────────────────────────────

export interface CourseDemand {
  courseType: string;
  coursesRun: number;
  totalEnrolments: number;
  avgAttendees: number;
  avgFillPct: number;
}

export interface CourseDemandResult {
  lookbackMonths: number;
  courseTypes: CourseDemand[];
  monthlyTrend: { year: number; month: number; courseType: string; enrolments: number }[];
}

export async function getCourseDemand(apiKey: string, months?: number): Promise<CourseDemandResult> {
  const qs = months ? `?months=${months}` : "";
  return apiRequest(`/api/analytics/course-demand${qs}`, apiKey);
}

// ── Transfers ────────────────────────────────────────────

export interface PendingTransfer {
  studentId: number;
  studentName: string;
  studentEmail: string;
  organisation: string | null;
  cancelledScheduleId: number;
  courseCode: string;
  courseTitle: string;
  originalStartDate: string | null;
  cancelledDate: string;
  daysSinceCancellation: number;
  reason: string | null;
}

export async function getPendingTransfers(apiKey: string): Promise<PendingTransfer[]> {
  return apiRequest("/api/transfers/pending", apiKey);
}

// ── Helpers ──────────────────────────────────────────────

export function formatCurrency(amount: number, currency = "GBP"): string {
  return new Intl.NumberFormat("en-GB", {
    style: "currency",
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}

export function formatDate(dateStr: string | null | undefined): string {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("en-GB", {
    weekday: "short", day: "numeric", month: "short",
  });
}

export function daysFromNow(dateStr: string): number {
  const now = new Date(); now.setHours(0, 0, 0, 0);
  const target = new Date(dateStr); target.setHours(0, 0, 0, 0);
  return Math.round((target.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
}
