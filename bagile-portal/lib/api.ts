const API_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.bagile.co.uk";

// ── Shared request helper ────────────────────────────────

// Convert snake_case keys to camelCase recursively
function toCamel(obj: any): any {
  if (Array.isArray(obj)) return obj.map(toCamel);
  if (obj !== null && typeof obj === "object") {
    return Object.fromEntries(
      Object.entries(obj).map(([k, v]) => [
        k.replace(/_([a-z])/g, (_, c) => c.toUpperCase()),
        toCamel(v),
      ])
    );
  }
  return obj;
}

async function apiRequest<T>(path: string, apiKey: string, options?: { method?: string; body?: unknown; timeoutMs?: number }): Promise<T> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), options?.timeoutMs ?? 15000);
  try {
    const res = await fetch(`${API_URL}${path}`, {
      method: options?.method || "GET",
      headers: {
        "X-Api-Key": apiKey,
        ...(options?.body ? { "Content-Type": "application/json" } : {}),
      },
      ...(options?.body ? { body: JSON.stringify(options.body) } : {}),
      signal: controller.signal,
    });
    if (!res.ok) throw new Error(`API error: ${res.status}`);
    return res.json();
  } finally {
    clearTimeout(timer);
  }
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
  /** Raw WooCommerce/platform lifecycle status (publish, cancelled, sold_out, draft).
   *  Sent as `courseStatus` by the monitoring endpoint — distinct from `status`
   *  on CourseScheduleItem which uses the same field name. */
  courseStatus?: string | null;
  /** Alias kept for backward compat — getCourseDisplayStatus reads `status`. */
  status?: string | null;
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
  byCountry: CountryRevenue[];
  previousYearYtdRevenue: number;
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

export interface CountryRevenue {
  region: string;
  country: string;
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
  const raw: any = await apiRequest(`/api/analytics/revenue${qs}`, apiKey);

  // Handle both old format (thisMonth/thisYear) and new CQRS format (currentMonthRevenue)
  if (raw.thisMonth !== undefined) {
    // Old minimal API format
    const monthly = (raw.monthlyBreakdown || []).map((m: any) => ({
      year: year || new Date().getFullYear(),
      month: m.month,
      monthName: ["", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"][m.month] || "",
      revenue: m.total || 0,
      orderCount: m.orders || 0,
      attendeeCount: 0,
    }));
    return {
      currentMonthRevenue: raw.thisMonth?.total || 0,
      currentYearRevenue: raw.thisYear?.total || 0,
      previousYearRevenue: 0,
      currentMonthOrders: raw.thisMonth?.orders || 0,
      currentYearOrders: raw.thisYear?.orders || 0,
      monthlyBreakdown: monthly,
      byCourseType: [],
      previousYearMonthly: [],
      bySource: [],
      byCountry: [],
      previousYearYtdRevenue: 0,
    };
  }

  return raw as RevenueSummary;
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
  const raw: any = await apiRequest(`/api/analytics/organisations${qs}`, apiKey);
  return { year: raw.year, organisations: (raw.organisations || []).map(toCamel) };
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

export async function getPartnerAnalytics(apiKey: string, year?: number): Promise<PartnerAnalytics[]> {
  const qs = year ? `?year=${year}` : "";
  const raw: any[] = await apiRequest(`/api/analytics/partners${qs}`, apiKey);
  return raw.map((p) => {
    const c = toCamel(p);
    // Compute tierMismatch client-side if not provided
    c.tierMismatch = c.tierMismatch ?? (c.ptnTier && c.calculatedTier && c.ptnTier !== c.calculatedTier);
    return c;
  });
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

// ── Course Schedules (full list with history) ────────────

export interface CourseScheduleItem {
  id: number;
  courseCode: string;
  title: string;
  startDate: string | null;
  endDate: string | null;
  location: string | null;
  trainerName: string | null;
  formatType: string | null;
  type: string | null;
  status: string | null;
  capacity: number | null;
  currentEnrolmentCount: number;
  guaranteedToRun: boolean;
  needsAttention: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export async function getCourseSchedules(
  apiKey: string,
  params: { from?: string; to?: string; courseCode?: string; trainer?: string; type?: string; status?: string; page?: number; pageSize?: number }
): Promise<PagedResult<CourseScheduleItem>> {
  const qs = new URLSearchParams();
  if (params.from) qs.set("from", params.from);
  if (params.to) qs.set("to", params.to);
  if (params.courseCode) qs.set("courseCode", params.courseCode);
  if (params.trainer) qs.set("trainer", params.trainer);
  if (params.type) qs.set("type", params.type);
  if (params.status) qs.set("status", params.status);
  qs.set("page", String(params.page || 1));
  qs.set("pageSize", String(params.pageSize || 50));
  return apiRequest(`/api/course-schedules?${qs}`, apiKey);
}

// ── Course Schedule Detail ───────────────────────────────

export interface CourseScheduleDetail extends CourseScheduleItem {
  capacity: number | null;
  price: number | null;
  sku: string | null;
  sourceSystem: string | null;
  lastSynced: string | null;
  clientOrganisationId: number | null;
  clientOrganisationName: string | null;
  clientOrganisationAcronym: string | null;
  invoiceReference: string | null;
  meetingUrl: string | null;
  meetingId: string | null;
  meetingPasscode: string | null;
  venueAddress: string | null;
  notes: string | null;
}

// ── Private Course Management ────────────────────────────

export interface CreatePrivateCourseRequest {
  name: string;
  courseCode: string;
  startDate: string;
  endDate: string;
  formatType: string;
  trainerName?: string;
  capacity?: number;
  price?: number;
  clientOrganisationId?: number;
  notes?: string;
  invoiceReference?: string;
  meetingUrl?: string;
  meetingId?: string;
  meetingPasscode?: string;
  venueAddress?: string;
}

export interface AttendeeInput {
  firstName: string;
  lastName: string;
  email: string;
  company?: string;
  country?: string;
}

export interface AddAttendeesResult {
  totalSubmitted: number;
  created: number;
  alreadyEnrolled: number;
  errors: string[];
}

export async function createPrivateCourse(apiKey: string, data: CreatePrivateCourseRequest): Promise<CourseScheduleDetail> {
  return apiRequest("/api/course-schedules/private", apiKey, { method: "POST", body: data });
}

export async function addPrivateAttendees(apiKey: string, courseId: number, attendees: AttendeeInput[]): Promise<AddAttendeesResult> {
  return apiRequest(`/api/course-schedules/${courseId}/attendees`, apiKey, {
    method: "POST",
    body: { attendees },
  });
}

export interface ScheduleConflict {
  conflictingCourseId: number;
  courseName: string;
  courseCode: string;
  startDate: string | null;
  endDate: string | null;
  type: string;
  trainerName: string | null;
  enrolmentCount: number;
  isGuaranteedToRun: boolean;
  conflictType: string;
}

export async function getScheduleConflicts(
  apiKey: string,
  startDate: string,
  endDate: string,
  trainer?: string
): Promise<ScheduleConflict[]> {
  const qs = new URLSearchParams({ startDate, endDate });
  if (trainer) qs.set("trainer", trainer);
  return apiRequest(`/api/course-schedules/conflicts?${qs}`, apiKey);
}

export async function parseAttendees(apiKey: string, rawText: string): Promise<AttendeeInput[]> {
  return apiRequest("/api/course-schedules/parse-attendees", apiKey, {
    method: "POST",
    body: { rawText },
  });
}

export async function getCourseScheduleDetail(apiKey: string, id: number): Promise<CourseScheduleDetail> {
  return apiRequest(`/api/course-schedules/${id}`, apiKey);
}

// ── Organisation Detail ─────────────────────────────────

export interface OrganisationDetail {
  name: string;
  primaryDomain: string | null;
  totalStudents: number;
  totalEnrolments: number;
  totalOrders: number;
  totalRevenue: number;
  firstOrderDate: string | null;
  lastOrderDate: string | null;
  lastCourseDate: string | null;
}

export interface OrganisationListItem {
  name: string;
  primaryDomain: string | null;
  totalStudents: number;
  totalEnrolments: number;
}

export interface OrgCourseHistory {
  courseCode: string;
  courseTitle: string;
  publicCount: number;
  privateCount: number;
  totalCount: number;
  lastRunDate: string | null;
}

export async function getOrganisationDetail(apiKey: string, name: string): Promise<OrganisationDetail> {
  return apiRequest(`/api/organisations/${encodeURIComponent(name)}`, apiKey);
}

export async function getOrganisationCourseHistory(apiKey: string, name: string): Promise<OrgCourseHistory[]> {
  return apiRequest(`/api/organisations/${encodeURIComponent(name)}/course-history`, apiKey);
}

export async function getOrganisations(apiKey: string, params?: { name?: string; page?: number; pageSize?: number }): Promise<PagedResult<OrganisationListItem>> {
  const qs = new URLSearchParams();
  if (params?.name) qs.set("name", params.name);
  qs.set("page", String(params?.page || 1));
  qs.set("pageSize", String(params?.pageSize || 50));
  return apiRequest(`/api/organisations?${qs}`, apiKey);
}

// ── Organisation Type-ahead ──────────────────────────────

export interface OrgSummary {
  id: number;
  name: string;
  acronym: string | null;
  partnerType: string | null;
  ptnTier: string | null;
}

/** Search the organisations table (name + aliases). Returns up to 10 results. */
export async function searchOrganisations(apiKey: string, q: string): Promise<OrgSummary[]> {
  if (!q.trim()) return [];
  const qs = new URLSearchParams({ q });
  return apiRequest(`/api/organisations/search?${qs}`, apiKey);
}

/** Create a new organisation row (from the portal type-ahead "Create as new" flow). */
export async function createOrganisation(apiKey: string, name: string, acronym?: string): Promise<OrgSummary> {
  return apiRequest("/api/organisations", apiKey, { method: "POST", body: { name, acronym } });
}

// ── Transfers by Course ──────────────────────────────────

export interface TransferRecord {
  studentId: number;
  studentName: string;
  studentEmail: string;
  fromCourseCode: string;
  fromCourseTitle: string;
  toCourseCode: string;
  toCourseTitle: string;
  reason: string | null;
  transferDate: string;
}

export interface TransfersByCourse {
  courseScheduleId: number;
  courseCode: string;
  courseTitle: string;
  transfersOut: TransferRecord[];
  transfersIn: TransferRecord[];
  totalTransfersOut: number;
  totalTransfersIn: number;
}

export async function getTransfersByCourse(apiKey: string, courseId: number): Promise<TransfersByCourse> {
  return apiRequest(`/api/transfers/by-course/${courseId}`, apiKey);
}

// ── Students ─────────────────────────────────────────────

export interface StudentListItem {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  company: string | null;
  createdAt: string;
}

export interface StudentDetail extends StudentListItem {
  totalEnrolments: number;
  lastCourseDate: string | null;
  updatedAt: string;
}

export interface StudentEnrolment {
  enrolmentId: number;
  courseScheduleId: number;
  courseCode: string;
  courseTitle: string;
  courseStartDate: string | null;
  status: string;
  type: string | null;
  enrolledAt: string;
}

export async function getStudents(
  apiKey: string,
  params?: { name?: string; email?: string; organisation?: string; page?: number; pageSize?: number }
): Promise<PagedResult<StudentListItem>> {
  const qs = new URLSearchParams();
  if (params?.name) qs.set("name", params.name);
  if (params?.email) qs.set("email", params.email);
  if (params?.organisation) qs.set("organisation", params.organisation);
  qs.set("page", String(params?.page || 1));
  qs.set("pageSize", String(params?.pageSize || 25));
  return apiRequest(`/api/students?${qs}`, apiKey);
}

export async function getStudentDetail(apiKey: string, id: number): Promise<StudentDetail> {
  return apiRequest(`/api/students/${id}`, apiKey);
}

export async function getStudentEnrolments(apiKey: string, id: number): Promise<StudentEnrolment[]> {
  return apiRequest(`/api/students/${id}/enrolments`, apiKey);
}

// ── Dashboard Overview ───────────────────────────────────

export interface DashboardOverview {
  revenue: RevenueSummary;
  monitoring: MonitoringCourse[];
  pendingTransferCount: number;
}

export async function getDashboardOverview(apiKey: string): Promise<DashboardOverview> {
  return apiRequest("/api/dashboard/overview", apiKey);
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

// ── Post-Course Templates ────────────────────────────────

export interface PostCourseTemplate {
  id: number;
  courseType: string;
  subjectTemplate: string;
  htmlBody: string;
  createdAt: string;
  updatedAt: string;
}

export async function listPostCourseTemplates(apiKey: string): Promise<PostCourseTemplate[]> {
  return apiRequest("/api/templates/post-course", apiKey);
}

export async function getPostCourseTemplate(apiKey: string, courseType: string): Promise<PostCourseTemplate> {
  return apiRequest(`/api/templates/post-course/${courseType}`, apiKey);
}

export async function upsertPostCourseTemplate(
  apiKey: string,
  courseType: string,
  subjectTemplate: string,
  htmlBody: string,
): Promise<PostCourseTemplate> {
  return apiRequest(`/api/templates/post-course/${courseType}`, apiKey, {
    method: "PUT",
    body: { subjectTemplate, htmlBody },
  });
}

export interface SendFollowUpResult {
  recipientCount: number;
  subject: string;
  courseType: string;
  recipientEmails: string[];
}

export async function getFollowUpEmailPreview(
  apiKey: string,
  courseScheduleId: number,
  htmlBody?: string,
): Promise<string> {
  const controller = new AbortController();
  setTimeout(() => controller.abort(), 15000);
  const res = await fetch(`${API_URL}/api/templates/post-course/preview/${courseScheduleId}`, {
    method: "POST",
    headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
    body: JSON.stringify({ htmlBody: htmlBody ?? null }),
    signal: controller.signal,
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.text();
}

export async function sendFollowUpEmail(
  apiKey: string,
  courseScheduleId: number,
  opts?: { courseTypeOverride?: string; htmlBodyOverride?: string },
): Promise<SendFollowUpResult> {
  return apiRequest(`/api/templates/post-course/send/${courseScheduleId}`, apiKey, {
    method: "POST",
    body: opts ?? {},
  });
}

export interface SendFollowUpTestResult {
  recipientEmail: string;
  subject: string;
  courseType: string;
}

export async function sendFollowUpTestEmail(
  apiKey: string,
  courseScheduleId: number,
  opts?: { courseTypeOverride?: string; htmlBodyOverride?: string; recipientEmail?: string },
): Promise<SendFollowUpTestResult> {
  return apiRequest(`/api/templates/post-course/test/${courseScheduleId}`, apiKey, {
    method: "POST",
    body: opts ?? {},
  });
}

// ── Student Override ──────────────────────────────────────

export interface UpdateStudentPayload {
  email?: string;
  firstName?: string;
  lastName?: string;
  company?: string;
  updatedBy?: string;
  overrideNote?: string;
}

export interface UpdateStudentResult {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  company: string | null;
  isOverridden: boolean;
  updatedBy: string | null;
  overrideNote: string | null;
}

export async function updateStudent(
  apiKey: string,
  studentId: number,
  payload: UpdateStudentPayload,
): Promise<UpdateStudentResult> {
  return apiRequest(`/api/students/${studentId}`, apiKey, {
    method: "PUT",
    body: payload,
  });
}

// ── Private Course Edit ───────────────────────────────────

export interface UpdatePrivateCourseRequest {
  name: string;
  trainerName?: string;
  startDate: string;
  endDate: string;
  capacity?: number;
  price?: number;
  clientOrganisationId?: number;
  invoiceReference?: string;
  venueAddress?: string;
  meetingUrl?: string;
  meetingId?: string;
  meetingPasscode?: string;
  notes?: string;
}

export async function updatePrivateCourse(
  apiKey: string,
  courseId: number,
  payload: UpdatePrivateCourseRequest,
): Promise<CourseScheduleDetail> {
  return apiRequest(`/api/course-schedules/${courseId}`, apiKey, {
    method: "PUT",
    body: payload,
  });
}

export async function removePrivateAttendee(
  apiKey: string,
  courseId: number,
  enrolmentId: number,
): Promise<void> {
  const res = await fetch(`${API_URL}/api/course-schedules/${courseId}/attendees/${enrolmentId}`, {
    method: "DELETE",
    headers: { "X-Api-Key": apiKey },
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
}

// ── Course Contacts ───────────────────────────────────────

export interface CourseContact {
  id: number;
  courseScheduleId: number;
  role: "admin" | "organiser" | "other";
  name: string;
  email: string;
  phone: string | null;
  createdAt: string;
}

export async function getCourseContacts(apiKey: string, courseId: number): Promise<CourseContact[]> {
  return apiRequest(`/api/course-schedules/${courseId}/contacts`, apiKey);
}

export interface AddCourseContactPayload {
  role: string;
  name: string;
  email: string;
  phone?: string;
}

export async function addCourseContact(
  apiKey: string,
  courseId: number,
  payload: AddCourseContactPayload,
): Promise<CourseContact> {
  return apiRequest(`/api/course-schedules/${courseId}/contacts`, apiKey, {
    method: "POST",
    body: payload,
  });
}

export async function deleteCourseContact(
  apiKey: string,
  courseId: number,
  contactId: number,
): Promise<void> {
  const res = await fetch(`${API_URL}/api/course-schedules/${courseId}/contacts/${contactId}`, {
    method: "DELETE",
    headers: { "X-Api-Key": apiKey },
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
}

export async function updateCourseContact(
  apiKey: string,
  courseId: number,
  contactId: number,
  payload: AddCourseContactPayload,
): Promise<CourseContact> {
  return apiRequest(`/api/course-schedules/${courseId}/contacts/${contactId}`, apiKey, {
    method: "PUT",
    body: payload,
  });
}

// ── Trainers ──────────────────────────────────────────────

export interface Trainer {
  id: number;
  name: string;
  email: string;
  phone?: string;
  isActive: boolean;
}

export async function getTrainers(apiKey: string): Promise<Trainer[]> {
  return apiRequest("/api/trainers", apiKey);
}

export async function createTrainer(
  apiKey: string,
  payload: { name: string; email: string; phone?: string },
): Promise<Trainer> {
  return apiRequest("/api/trainers", apiKey, { method: "POST", body: payload });
}

export async function updateTrainer(
  apiKey: string,
  id: number,
  payload: { name: string; email: string; phone?: string },
): Promise<Trainer> {
  return apiRequest(`/api/trainers/${id}`, apiKey, { method: "PUT", body: payload });
}

export async function deleteTrainer(apiKey: string, id: number): Promise<void> {
  const res = await fetch(`${API_URL}/api/trainers/${id}`, {
    method: "DELETE",
    headers: { "X-Api-Key": apiKey },
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
}

// ── Pre-Course Templates ──────────────────────────────────

export interface PreCourseTemplate {
  id: number;
  courseType: string;
  format: string;
  subjectTemplate: string;
  htmlBody: string;
  updatedAt: string;
}

export async function getPreCourseTemplates(apiKey: string): Promise<PreCourseTemplate[]> {
  return apiRequest("/api/templates/pre-course", apiKey);
}

export async function getPreCourseTemplate(apiKey: string, courseType: string, format: string): Promise<PreCourseTemplate> {
  return apiRequest(`/api/templates/pre-course/${courseType}?format=${encodeURIComponent(format)}`, apiKey);
}

export async function updatePreCourseTemplate(
  apiKey: string,
  courseType: string,
  data: { format: string; subjectTemplate: string; htmlBody: string },
): Promise<void> {
  return apiRequest(`/api/templates/pre-course/${courseType}`, apiKey, { method: "PUT", body: data });
}

export async function getPreCourseEmailPreview(
  apiKey: string,
  courseScheduleId: number,
  htmlBody?: string,
): Promise<string> {
  const controller = new AbortController();
  setTimeout(() => controller.abort(), 15000);
  const res = await fetch(`${API_URL}/api/templates/pre-course/preview/${courseScheduleId}`, {
    method: "POST",
    headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
    body: JSON.stringify({ htmlBody: htmlBody ?? null }),
    signal: controller.signal,
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.text();
}

export async function sendPreCourseEmail(
  apiKey: string,
  courseScheduleId: number,
  data?: { htmlBodyOverride?: string; additionalCc?: string[] },
): Promise<{ recipientCount: number }> {
  return apiRequest(`/api/templates/pre-course/send/${courseScheduleId}`, apiKey, { method: "POST", body: data ?? {} });
}

export async function sendPreCourseTestEmail(
  apiKey: string,
  courseScheduleId: number,
  data?: { htmlBodyOverride?: string; recipientEmail?: string },
): Promise<{ recipientEmail: string }> {
  return apiRequest(`/api/templates/pre-course/test/${courseScheduleId}`, apiKey, { method: "POST", body: data ?? {} });
}

// ── Email Send Log ────────────────────────────────────────

export interface EmailSendLog {
  id: number;
  templateType: string;
  sentBy: string;
  recipientCount: number;
  subject: string;
  isTest: boolean;
  sentAt: string;
}

export async function getEmailSendLog(apiKey: string, courseScheduleId: number): Promise<EmailSendLog[]> {
  return apiRequest(`/api/course-schedules/${courseScheduleId}/email-log`, apiKey);
}
