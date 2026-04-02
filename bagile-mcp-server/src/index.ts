#!/usr/bin/env node
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { apiGet, apiPost, apiPut } from "./api-client.js";
import { formatResult } from "./utils.js";

const server = new McpServer({
  name: "bagile-api",
  version: "1.0.0",
});

// --- Pagination helpers ---
const paginationParams = {
  page: z.number().min(1).optional().describe("Page number (default 1)"),
  pageSize: z.number().min(1).max(100).optional().describe("Items per page (1-100, default 20)"),
};

// ============================================================
// COURSE SCHEDULES
// ============================================================

server.tool(
  "list_course_schedules",
  "List course schedules with optional filters (date range, course code, trainer, type, status)",
  {
    from: z.string().optional().describe("Filter from date (YYYY-MM-DD)"),
    to: z.string().optional().describe("Filter to date (YYYY-MM-DD)"),
    courseCode: z.string().optional().describe("Course code filter (e.g. PSM, PSPO)"),
    trainer: z.string().optional().describe("Trainer name filter"),
    type: z.string().optional().describe("Course type filter"),
    status: z.string().optional().describe("Course status filter"),
    ...paginationParams,
  },
  async (params) => {
    const result = await apiGet("/api/course-schedules", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_course_schedule",
  "Get details of a specific course schedule by ID",
  {
    id: z.number().describe("Course schedule ID"),
  },
  async ({ id }) => {
    const result = await apiGet(`/api/course-schedules/${id}`);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_course_attendees",
  "Get attendees for a specific course schedule",
  {
    id: z.number().describe("Course schedule ID"),
  },
  async ({ id }) => {
    const result = await apiGet(`/api/course-schedules/${id}/attendees`);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// ENROLMENTS
// ============================================================

server.tool(
  "list_enrolments",
  "List enrolments with optional filters (course, student, status, organisation, date range)",
  {
    courseScheduleId: z.number().optional().describe("Filter by course schedule ID"),
    studentId: z.number().optional().describe("Filter by student ID"),
    status: z.string().optional().describe("Enrolment status filter"),
    organisation: z.string().optional().describe("Organisation name filter"),
    from: z.string().optional().describe("Filter from date (YYYY-MM-DD)"),
    to: z.string().optional().describe("Filter to date (YYYY-MM-DD)"),
    ...paginationParams,
  },
  async (params) => {
    const result = await apiGet("/api/enrolments", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// ORDERS
// ============================================================

server.tool(
  "list_orders",
  "List orders with optional filters (status, date range, email)",
  {
    status: z.string().optional().describe("Order status (completed, pending, processing, cancelled)"),
    from: z.string().optional().describe("Filter from date (YYYY-MM-DD)"),
    to: z.string().optional().describe("Filter to date (YYYY-MM-DD)"),
    email: z.string().optional().describe("Customer email filter"),
    ...paginationParams,
  },
  async (params) => {
    const result = await apiGet("/api/orders", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_order",
  "Get details of a specific order by ID",
  {
    id: z.number().describe("Order ID"),
  },
  async ({ id }) => {
    const result = await apiGet(`/api/orders/${id}`);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// ORGANISATIONS
// ============================================================

server.tool(
  "list_organisations",
  "List organisations with optional filters (name, domain)",
  {
    name: z.string().optional().describe("Organisation name filter"),
    domain: z.string().optional().describe("Organisation domain filter"),
    ...paginationParams,
  },
  async (params) => {
    const result = await apiGet("/api/organisations", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_organisation",
  "Get details of a specific organisation by name",
  {
    name: z.string().describe("Organisation name"),
  },
  async ({ name }) => {
    const result = await apiGet(`/api/organisations/${encodeURIComponent(name)}`);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_organisation_course_history",
  "Get course history for a specific organisation",
  {
    name: z.string().describe("Organisation name"),
  },
  async ({ name }) => {
    const result = await apiGet(`/api/organisations/${encodeURIComponent(name)}/course-history`);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// STUDENTS
// ============================================================

server.tool(
  "list_students",
  "List students with optional filters (email, name, organisation, course code)",
  {
    email: z.string().optional().describe("Student email filter"),
    name: z.string().optional().describe("Student name filter"),
    organisation: z.string().optional().describe("Organisation name filter"),
    courseCode: z.string().optional().describe("Course code filter"),
    ...paginationParams,
  },
  async (params) => {
    const result = await apiGet("/api/students", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_student",
  "Get details of a specific student by ID",
  {
    id: z.number().describe("Student ID"),
  },
  async ({ id }) => {
    const result = await apiGet(`/api/students/${id}`);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_student_enrolments",
  "Get enrolment timeline for a specific student",
  {
    id: z.number().describe("Student ID"),
  },
  async ({ id }) => {
    const result = await apiGet(`/api/students/${id}/enrolments`);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// TRANSFERS
// ============================================================

server.tool(
  "list_transfers",
  "List transfers with optional filters (date range, reason, organisation, course)",
  {
    from: z.string().optional().describe("Filter from date (YYYY-MM-DD)"),
    to: z.string().optional().describe("Filter to date (YYYY-MM-DD)"),
    reason: z.string().optional().describe("Transfer reason filter"),
    organisationName: z.string().optional().describe("Organisation name filter"),
    courseScheduleId: z.number().optional().describe("Course schedule ID filter"),
    ...paginationParams,
  },
  async (params) => {
    const result = await apiGet("/api/transfers", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_pending_transfers",
  "Get pending transfers (students cancelled but not yet rebooked)",
  {},
  async () => {
    const result = await apiGet("/api/transfers/pending");
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_transfers_by_course",
  "Get transfers in and out of a specific course schedule",
  {
    scheduleId: z.number().describe("Course schedule ID"),
  },
  async ({ scheduleId }) => {
    const result = await apiGet(`/api/transfers/by-course/${scheduleId}`);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// COURSE MONITORING
// ============================================================

server.tool(
  "get_course_monitoring",
  "Get course monitoring data — enrolment vs minimums, decision deadlines, recommended actions for upcoming courses. Returns courses sorted by urgency (most urgent first).",
  {
    daysAhead: z.number().min(1).max(90).optional().describe("Number of days ahead to monitor (default 30)"),
  },
  async (params) => {
    const result = await apiGet("/api/course-schedules/monitoring", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "cancel_course",
  "Cancel a course schedule. Idempotent — cancelling an already-cancelled course returns its current state.",
  {
    id: z.number().describe("Course schedule ID to cancel"),
    reason: z.string().optional().describe("Reason for cancellation"),
  },
  async ({ id, reason }) => {
    const result = await apiPost(`/api/course-schedules/${id}/cancel`, { reason: reason || "" });
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// STUDENT MANAGEMENT
// ============================================================

server.tool(
  "update_student",
  "Update a student's details (email, first name, last name, company). Changes are platform-only — no side effects on FooEvents tickets or WooCommerce. Fields marked as overridden are protected from ETL re-sync.",
  {
    id: z.number().describe("Student ID"),
    email: z.string().optional().describe("New email address"),
    firstName: z.string().optional().describe("New first name"),
    lastName: z.string().optional().describe("New last name"),
    company: z.string().optional().describe("New company / organisation name"),
    updatedBy: z.string().optional().describe("Who made the change (e.g. 'alex@bagile.co.uk')"),
    overrideNote: z.string().optional().describe("Reason for the override (e.g. 'Corrected PTN partner email — real attendee is at Ofgem')"),
  },
  async ({ id, email, firstName, lastName, company, updatedBy, overrideNote }) => {
    const body: Record<string, unknown> = {};
    if (email !== undefined) body.email = email;
    if (firstName !== undefined) body.firstName = firstName;
    if (lastName !== undefined) body.lastName = lastName;
    if (company !== undefined) body.company = company;
    if (updatedBy !== undefined) body.updatedBy = updatedBy;
    if (overrideNote !== undefined) body.overrideNote = overrideNote;
    const result = await apiPut(`/api/students/${id}`, body);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// POST-COURSE EMAILS
// ============================================================

server.tool(
  "send_post_course_email",
  "Send the post-course follow-up email to all active attendees of a course schedule. Uses the seeded template for the course type. CC's info@bagile.co.uk. Returns recipient count and emails sent.",
  {
    courseScheduleId: z.number().describe("Course schedule ID to send the follow-up for"),
    courseTypeOverride: z.string().optional().describe("Override the template to use (e.g. 'PSPO' instead of auto-detected type)"),
    delayNote: z.string().optional().describe("Optional delay apology note inserted into the email body (e.g. 'Apologies for the delay in sending these through.')"),
  },
  async ({ courseScheduleId, courseTypeOverride, delayNote }) => {
    const body: Record<string, unknown> = {};
    if (courseTypeOverride !== undefined) body.courseTypeOverride = courseTypeOverride;
    if (delayNote !== undefined) body.delayNote = delayNote;
    const result = await apiPost(`/api/templates/post-course/send/${courseScheduleId}`, body);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// ANALYTICS
// ============================================================

server.tool(
  "get_revenue_summary",
  "Get revenue summary — monthly breakdown, year-on-year comparison (fair YTD), by course type, by source (public/private), by country/region",
  {
    year: z.number().optional().describe("Year to analyse (default: current year)"),
  },
  async (params) => {
    const result = await apiGet("/api/analytics/revenue", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_revenue_month_drilldown",
  "Get detailed revenue for a specific month — every order with company, payment method, course, attendees",
  {
    year: z.number().describe("Year"),
    month: z.number().min(1).max(12).describe("Month (1-12)"),
  },
  async ({ year, month }) => {
    const result = await apiGet(`/api/analytics/revenue/${year}/${month}`);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_organisation_analytics",
  "Get top organisations by spend, bookings, and delegates for a given year",
  {
    year: z.number().optional().describe("Year (default: current)"),
    sortBy: z.string().optional().describe("Sort by: spend, bookings, delegates (default: spend)"),
  },
  async (params) => {
    const result = await apiGet("/api/analytics/organisations", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_repeat_customers",
  "Get repeat customers — companies with multiple bookings, lifetime spend, relationship duration",
  {
    year: z.number().optional().describe("Year for 'this year' metrics (default: current)"),
    minBookings: z.number().optional().describe("Minimum bookings to include (default: 2)"),
  },
  async (params) => {
    const result = await apiGet("/api/analytics/organisations/repeat-customers", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_partner_analytics",
  "Get PTN partner tier tracking — current vs calculated tier, bookings, delegates, spend, mismatch flags",
  {
    year: z.number().optional().describe("Year (default: current)"),
  },
  async (params) => {
    const result = await apiGet("/api/analytics/partners", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

server.tool(
  "get_course_demand",
  "Get course demand analytics — which course types sell best, fill rates, monthly trends",
  {
    months: z.number().optional().describe("Lookback period in months (default: 12)"),
  },
  async (params) => {
    const result = await apiGet("/api/analytics/course-demand", params);
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// HEALTH CHECK
// ============================================================

server.tool(
  "health_check",
  "Check if the BAgile API is healthy and responding",
  {},
  async () => {
    const result = await apiGet("/health");
    return { content: [{ type: "text" as const, text: formatResult(result) }] };
  }
);

// ============================================================
// START SERVER
// ============================================================

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
}

main().catch((err) => {
  console.error("Fatal error:", err);
  process.exit(1);
});
