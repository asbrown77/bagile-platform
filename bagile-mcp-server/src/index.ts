#!/usr/bin/env node
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { apiGet } from "./api-client.js";
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
