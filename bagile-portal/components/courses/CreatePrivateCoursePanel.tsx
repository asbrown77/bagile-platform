"use client";

import { useState } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { createPrivateCourse, CreatePrivateCourseRequest } from "@/lib/api";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  onCreated: () => void;
}

const COURSE_CODES = ["PSM", "PSPO", "PSPOAI", "PSPOA", "PAL", "PALE", "PSK", "PSMA", "PSMAI", "PSFS", "APS", "EBM"];

export function CreatePrivateCoursePanel({ open, onClose, apiKey, onCreated }: Props) {
  const [form, setForm] = useState<CreatePrivateCourseRequest>({
    name: "",
    courseCode: "PSM",
    startDate: "",
    endDate: "",
    formatType: "virtual",
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  function update(field: string, value: string | number | undefined) {
    setForm((f) => ({ ...f, [field]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name || !form.startDate || !form.endDate) {
      setError("Name, start date, and end date are required");
      return;
    }
    setError("");
    setSaving(true);
    try {
      const created = await createPrivateCourse(apiKey, form);
      setForm({ name: "", courseCode: "PSM", startDate: "", endDate: "", formatType: "virtual" });
      onCreated();
      onClose();
      // Redirect to the new course detail page where they can add attendees
      window.location.href = `/courses/${created.id}`;
    } catch (err) {
      setError("Failed to create course");
    } finally {
      setSaving(false);
    }
  }

  const isVirtual = form.formatType === "virtual";

  return (
    <SlideOver open={open} onClose={onClose} title="Create Private Course" subtitle="Add a new private/corporate course" wide>
      <form onSubmit={handleSubmit} className="space-y-5">
        {error && <AlertBanner variant="danger">{error}</AlertBanner>}

        {/* Course basics */}
        <div className="grid grid-cols-2 gap-4">
          <div className="col-span-2">
            <label className="block text-xs font-medium text-gray-700 mb-1">Course Name</label>
            <input type="text" value={form.name} onChange={(e) => update("name", e.target.value)}
              placeholder="e.g. PSM - Acme Corp" required
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Course Type</label>
            <select value={form.courseCode} onChange={(e) => update("courseCode", e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white">
              {COURSE_CODES.map((c) => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Format</label>
            <select value={form.formatType} onChange={(e) => update("formatType", e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white">
              <option value="virtual">Virtual</option>
              <option value="in_person">In-person</option>
            </select>
          </div>
        </div>

        {/* Dates */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Start Date</label>
            <input type="date" value={form.startDate} onChange={(e) => update("startDate", e.target.value)} required
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">End Date</label>
            <input type="date" value={form.endDate} onChange={(e) => update("endDate", e.target.value)} required
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
        </div>

        {/* Trainer + capacity + price */}
        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Trainer</label>
            <input type="text" value={form.trainerName || ""} onChange={(e) => update("trainerName", e.target.value)}
              placeholder="Alex Brown"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Capacity</label>
            <input type="number" value={form.capacity || ""} onChange={(e) => update("capacity", e.target.value ? Number(e.target.value) : undefined)}
              placeholder="20"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Price (total)</label>
            <input type="number" value={form.price || ""} onChange={(e) => update("price", e.target.value ? Number(e.target.value) : undefined)}
              placeholder="5000"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
        </div>

        {/* Invoice reference */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Invoice Reference (Xero)</label>
          <input type="text" value={form.invoiceReference || ""} onChange={(e) => update("invoiceReference", e.target.value)}
            placeholder="INV-00123"
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
        </div>

        {/* Virtual: meeting details */}
        {isVirtual && (
          <div className="bg-blue-50 rounded-lg p-4 space-y-3">
            <p className="text-xs font-semibold text-blue-800 uppercase tracking-wide">Meeting Details</p>
            <div>
              <label className="block text-xs font-medium text-blue-700 mb-1">Zoom/Teams URL</label>
              <input type="url" value={form.meetingUrl || ""} onChange={(e) => update("meetingUrl", e.target.value)}
                placeholder="https://zoom.us/j/..."
                className="w-full border border-blue-200 rounded-lg px-3 py-2 text-sm bg-white" />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-blue-700 mb-1">Meeting ID</label>
                <input type="text" value={form.meetingId || ""} onChange={(e) => update("meetingId", e.target.value)}
                  placeholder="123 456 7890"
                  className="w-full border border-blue-200 rounded-lg px-3 py-2 text-sm bg-white" />
              </div>
              <div>
                <label className="block text-xs font-medium text-blue-700 mb-1">Passcode</label>
                <input type="text" value={form.meetingPasscode || ""} onChange={(e) => update("meetingPasscode", e.target.value)}
                  placeholder="abc123"
                  className="w-full border border-blue-200 rounded-lg px-3 py-2 text-sm bg-white" />
              </div>
            </div>
          </div>
        )}

        {/* In-person: venue */}
        {!isVirtual && (
          <div className="bg-amber-50 rounded-lg p-4 space-y-3">
            <p className="text-xs font-semibold text-amber-800 uppercase tracking-wide">Venue Details</p>
            <div>
              <label className="block text-xs font-medium text-amber-700 mb-1">Venue Address</label>
              <textarea value={form.venueAddress || ""} onChange={(e) => update("venueAddress", e.target.value)}
                placeholder="Conference Room 3, 10 Downing Street, London SW1A 2AA"
                rows={2}
                className="w-full border border-amber-200 rounded-lg px-3 py-2 text-sm bg-white" />
            </div>
          </div>
        )}

        {/* Notes */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Notes</label>
          <textarea value={form.notes || ""} onChange={(e) => update("notes", e.target.value)}
            placeholder="Any additional details..."
            rows={2}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
        </div>

        {/* Submit */}
        <div className="flex gap-3 pt-4 border-t border-gray-200">
          <Button type="submit" disabled={saving}>
            {saving ? "Creating..." : "Create Course"}
          </Button>
          <Button variant="secondary" type="button" onClick={onClose}>Cancel</Button>
        </div>
      </form>
    </SlideOver>
  );
}
