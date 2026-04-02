"use client";

import { useState } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import {
  createPrivateCourse,
  addPrivateAttendees,
  CreatePrivateCourseRequest,
  AttendeeInput,
  OrgSummary,
} from "@/lib/api";
import { generateCourseName, generateInvoiceRef } from "@/lib/privateCourseHelpers";
import { Copy, FileJson } from "lucide-react";
import { PrivateCourseForm } from "./PrivateCourseForm";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  onCreated: () => void;
}

const JSON_TEMPLATE = `{
  "organisationName": "Frazer-Nash Consultancy Ltd",
  "organisationAcronym": "FNC",
  "courseCode": "PSM",
  "startDate": "2026-04-15",
  "endDate": "2026-04-16",
  "formatType": "virtual",
  "trainerName": "Alex Brown",
  "capacity": 20,
  "price": 5000,
  "meetingUrl": "https://zoom.us/j/123456789",
  "meetingId": "123 456 789",
  "meetingPasscode": "abc123",
  "venueAddress": "",
  "notes": "",
  "attendees": [
    { "firstName": "John", "lastName": "Smith", "email": "john@company.com" },
    { "firstName": "Jane", "lastName": "Doe", "email": "jane@company.com" }
  ]
}`;

export function CreatePrivateCoursePanel({ open, onClose, apiKey, onCreated }: Props) {
  const [mode, setMode] = useState<"form" | "json">("form");
  const [jsonText, setJsonText] = useState("");
  const [jsonParsed, setJsonParsed] = useState<{
    course: CreatePrivateCourseRequest;
    attendees: AttendeeInput[];
    org?: OrgSummary;
  } | null>(null);
  const [jsonSaving, setJsonSaving] = useState(false);
  const [jsonError, setJsonError] = useState("");
  const [copied, setCopied] = useState(false);

  function copyTemplate() {
    navigator.clipboard.writeText(JSON_TEMPLATE);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  function parseJson() {
    try {
      const data = JSON.parse(jsonText);
      const orgFromJson: OrgSummary | undefined = data.organisationName
        ? {
            id: 0,
            name: data.organisationName,
            acronym: data.organisationAcronym ?? null,
            partnerType: null,
            ptnTier: null,
          }
        : undefined;
      const course: CreatePrivateCourseRequest = {
        name:
          data.name ||
          generateCourseName(
            data.courseCode || "PSM",
            data.organisationName || "",
            data.formatType || "virtual"
          ),
        courseCode: data.courseCode || "PSM",
        startDate: data.startDate || "",
        endDate: data.endDate || "",
        formatType: data.formatType || "virtual",
        trainerName: data.trainerName,
        capacity: data.capacity,
        price: data.price,
        invoiceReference:
          data.invoiceReference ||
          generateInvoiceRef(
            data.courseCode || "PSM",
            data.organisationAcronym || "",
            data.startDate || ""
          ),
        meetingUrl: data.meetingUrl,
        meetingId: data.meetingId,
        meetingPasscode: data.meetingPasscode,
        venueAddress: data.venueAddress,
        notes: data.notes,
      };
      const atts: AttendeeInput[] = (data.attendees || []).map(
        (a: AttendeeInput & { [key: string]: string }) => ({
          firstName: a.firstName || "",
          lastName: a.lastName || "",
          email: a.email || "",
          company: a.company,
          country: a.country,
        })
      );
      setJsonParsed({ course, attendees: atts, org: orgFromJson });
      setJsonError("");
    } catch {
      setJsonError("Invalid JSON — check the format and try again");
      setJsonParsed(null);
    }
  }

  async function handleJsonSubmit() {
    if (!jsonParsed || !apiKey) return;
    setJsonSaving(true);
    setJsonError("");
    try {
      const payload: CreatePrivateCourseRequest = {
        ...jsonParsed.course,
        clientOrganisationId:
          jsonParsed.org?.id && jsonParsed.org.id > 0
            ? jsonParsed.org.id
            : undefined,
      };
      const created = await createPrivateCourse(apiKey, payload);
      if (jsonParsed.attendees.length > 0) {
        const valid = jsonParsed.attendees.filter((a) => a.email.trim());
        if (valid.length > 0) await addPrivateAttendees(apiKey, created.id, valid);
      }
      onCreated();
      onClose();
      window.location.href = `/courses/${created.id}`;
    } catch {
      setJsonError("Failed to create course");
    } finally {
      setJsonSaving(false);
    }
  }

  return (
    <SlideOver
      open={open}
      onClose={onClose}
      title="Create Private Course"
      subtitle="Add a new private/corporate course"
      wide
    >
      {/* ── JSON Import Mode ── */}
      {mode !== "json" && (
        <div className="mb-4">
          <button
            onClick={() => setMode("json")}
            className="text-xs text-gray-400 hover:text-brand-600 underline"
          >
            Advanced: Import from JSON
          </button>
        </div>
      )}

      {mode === "json" && (
        <div className="space-y-4">
          <div className="bg-gray-50 rounded-lg p-4">
            <div className="flex items-center justify-between mb-2">
              <p className="text-xs font-semibold text-gray-700 uppercase tracking-wide">
                JSON Template
              </p>
              <button
                onClick={copyTemplate}
                className="flex items-center gap-1.5 text-xs text-brand-600 hover:text-brand-700 font-medium"
              >
                <Copy className="w-3.5 h-3.5" />
                {copied ? "Copied!" : "Copy Template"}
              </button>
            </div>
            <p className="text-xs text-gray-500 mb-2">
              Copy this template, fill in the client details, then paste below.
            </p>
            <pre className="bg-gray-900 text-gray-300 rounded-lg p-3 text-xs overflow-x-auto max-h-40 overflow-y-auto">
              {JSON_TEMPLATE}
            </pre>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Paste JSON</label>
            <textarea
              value={jsonText}
              onChange={(e) => {
                setJsonText(e.target.value);
                setJsonParsed(null);
              }}
              placeholder="Paste the filled-in JSON here..."
              rows={8}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono"
            />
            <Button variant="secondary" size="sm" onClick={parseJson} className="mt-2">
              <FileJson className="w-3.5 h-3.5" /> Parse and Preview
            </Button>
          </div>

          {jsonError && (
            <AlertBanner variant="danger">{jsonError}</AlertBanner>
          )}

          {jsonParsed && (
            <div className="bg-green-50 rounded-lg p-4 space-y-3">
              <p className="text-xs font-semibold text-green-800 uppercase tracking-wide">Preview</p>
              <div className="text-sm text-green-900 space-y-1">
                <p><strong>{jsonParsed.course.name}</strong></p>
                <p>
                  {jsonParsed.course.courseCode} — {jsonParsed.course.formatType} —{" "}
                  {jsonParsed.course.startDate} to {jsonParsed.course.endDate}
                </p>
                {jsonParsed.org && (
                  <p>
                    Organisation: {jsonParsed.org.name}
                    {jsonParsed.org.acronym ? ` (${jsonParsed.org.acronym})` : ""}
                  </p>
                )}
                {jsonParsed.course.trainerName && (
                  <p>Trainer: {jsonParsed.course.trainerName}</p>
                )}
                {jsonParsed.course.invoiceReference && (
                  <p>Reference: {jsonParsed.course.invoiceReference}</p>
                )}
                {jsonParsed.attendees.length > 0 && (
                  <div className="mt-2">
                    <p className="font-medium">{jsonParsed.attendees.length} attendees:</p>
                    <ul className="ml-4 text-xs space-y-0.5">
                      {jsonParsed.attendees.map((a, i) => (
                        <li key={i}>
                          {a.firstName} {a.lastName} — {a.email}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
              <div className="flex gap-3 pt-3 border-t border-green-200">
                <Button
                  onClick={handleJsonSubmit}
                  disabled={jsonSaving || !apiKey}
                >
                  {jsonSaving
                    ? "Creating..."
                    : `Create Course${
                        jsonParsed.attendees.length > 0
                          ? ` + ${jsonParsed.attendees.length} Attendees`
                          : ""
                      }`}
                </Button>
              </div>
            </div>
          )}

          <button
            onClick={() => setMode("form")}
            className="text-xs text-gray-400 hover:text-brand-600 underline mt-4"
          >
            Back to form
          </button>
        </div>
      )}

      {/* ── Form Mode ── */}
      {mode === "form" && (
        <PrivateCourseForm
          mode="create"
          apiKey={apiKey}
          onSuccess={onCreated}
          onCancel={onClose}
        />
      )}
    </SlideOver>
  );
}
