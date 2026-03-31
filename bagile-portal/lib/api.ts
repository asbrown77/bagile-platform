const API_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.bagile.co.uk";

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

export async function loginWithGoogle(
  idToken: string
): Promise<{ token: string; email: string; name: string; apiKey?: string }> {
  const res = await fetch(`${API_URL}/portal/auth/google`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ idToken }),
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || `Login failed (${res.status})`);
  }

  return res.json();
}

export async function listKeys(token: string): Promise<ApiKey[]> {
  const res = await fetch(`${API_URL}/portal/keys`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to list keys");
  return res.json();
}

export async function createKey(
  token: string,
  label: string
): Promise<CreateKeyResponse> {
  const res = await fetch(`${API_URL}/portal/keys`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ label }),
  });
  if (!res.ok) throw new Error("Failed to create key");
  return res.json();
}

export async function revokeKey(
  token: string,
  id: string
): Promise<void> {
  const res = await fetch(`${API_URL}/portal/keys/${id}`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to revoke key");
}

// --- Dashboard API (uses API key from portal JWT) ---

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
}

export interface OrderSummary {
  totalCount: number;
  items: Array<{
    id: number;
    externalId: string;
    status: string;
    totalAmount: number;
    orderDate: string;
    customerName: string;
  }>;
}

async function apiCall(path: string, apiKey: string) {
  const res = await fetch(`${API_URL}${path}`, {
    headers: { "X-Api-Key": apiKey },
  });
  if (!res.ok) throw new Error(`API error: ${res.status}`);
  return res.json();
}

export async function getMonitoring(apiKey: string, daysAhead = 60): Promise<MonitoringCourse[]> {
  return apiCall(`/api/course-schedules/monitoring?daysAhead=${daysAhead}`, apiKey);
}

export async function getCourseAttendees(apiKey: string, courseId: number): Promise<CourseAttendee[]> {
  return apiCall(`/api/course-schedules/${courseId}/attendees`, apiKey);
}

export async function getOrders(apiKey: string, params: { status?: string; from?: string; to?: string; pageSize?: number }): Promise<OrderSummary> {
  const qs = new URLSearchParams();
  if (params.status) qs.set("status", params.status);
  if (params.from) qs.set("from", params.from);
  if (params.to) qs.set("to", params.to);
  qs.set("pageSize", String(params.pageSize || 100));
  return apiCall(`/api/orders?${qs}`, apiKey);
}

export function getExportUrl(courseId: number, apiKey: string): string {
  return `${API_URL}/api/course-schedules/${courseId}/attendees/export`;
}

export interface RevenueData {
  thisMonth: { total: number; orders: number };
  thisYear: { total: number; orders: number };
  monthlyBreakdown: Array<{ month: number; total: number; orders: number }>;
}

export async function getRevenue(apiKey: string): Promise<RevenueData> {
  return apiCall("/api/analytics/revenue", apiKey);
}

export async function getPendingTransfers(apiKey: string): Promise<any[]> {
  return apiCall("/api/transfers/pending", apiKey);
}
