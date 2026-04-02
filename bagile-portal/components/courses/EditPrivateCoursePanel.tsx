"use client";

import { useState, useEffect } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { CourseScheduleDetail, UpdatePrivateCourseRequest, updatePrivateCourse } from "@/lib/api";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  course: CourseScheduleDetail;
  onSaved: () => void;
}

function toDateInput(iso: string | null | undefined): string {
  if (!iso) return "";
  return iso.slice(0, 10);
}

export function EditPrivateCoursePanel({ open, onClose, apiKey, course, onSaved }: Props) {
  const [form, setForm] = useState<UpdatePrivateCourseRequest>({
    name: "",
    startDate: "",
    endDate: "",
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const isVirtual = (course.formatType ?? "").toLowerCase().includes("virtual");

  // Seed form from course whenever the panel opens
  useEffect(() => {
    if (!open) return;
    setForm({
      name: course.title ?? "",
      trainerName: course.trainerName ?? undefined,
      startDate: toDateInput(course.startDate),
      endDate: toDateInput(course.endDate ?? course.startDate),
      capacity: course.capacity ?? undefined,
      price: course.price ?? undefined,
      invoiceReference: course.invoiceReference ?? undefined,
      venueAddress: course.venueAddress ?? undefined,
      meetingUrl: course.meetingUrl ?? undefined,
      meetingId: course.meetingId ?? undefined,
      meetingPasscode: course.meetingPasscode ?? undefined,
      notes: course.notes ?? undefined,
    });
    setError("");
  }, [open, course]);

  function update(field: keyof UpdatePrivateCourseRequest, value: string | number | undefined) {
    setForm((f) => ({ ...f, [field]: value }));
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!form.name || !form.startDate || !form.endDate) {
      setError("Name, start date, and end date are required");
      return;
    }
    setSaving(true);
    setError("");
    try {
      await updatePrivateCourse(apiKey, course.id, form);
      onSaved();
      onClose();
    } catch {
      setError("Failed to save changes — please try again");
    } finally {
      setSaving(false);
    }
  }

  return (
    <SlideOver open={open} onClose={onClose} title="Edit Course" subtitle={course.courseCode ?? undefined}>
      {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}

      <form onSubmit={handleSubmit} className="space-y-5">
        {/* Name */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Course Name</label>
          <input
            type="text"
            value={form.name}
            onChange={(e) => update("name", e.target.value)}
            required
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
          />
        </div>

        {/* Trainer */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Trainer</label>
          <input
            type="text"
            value={form.trainerName ?? ""}
            onChange={(e) => update("trainerName", e.target.value || undefined)}
            placeholder="Alex Brown"
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
        </div>

        {/* Dates */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Start Date</label>
            <input
              type="date"
              value={form.startDate}
              onChange={(e) => update("startDate", e.target.value)}
              required
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">End Date</label>
            <input
              type="date"
              value={form.endDate}
              onChange={(e) => update("endDate", e.target.value)}
              required
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>
        </div>

        {/* Capacity + Price */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Capacity</label>
            <input
              type="number"
              value={form.capacity ?? ""}
              onChange={(e) => update("capacity", e.target.value ? Number(e.target.value) : undefined)}
              placeholder="20"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Price (total £)</label>
            <input
              type="number"
              value={form.price ?? ""}
              onChange={(e) => update("price", e.target.value ? Number(e.target.value) : undefined)}
              placeholder="5000"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>
        </div>

        {/* Invoice Reference */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Invoice Reference (Xero)</label>
          <input
            type="text"
            value={form.invoiceReference ?? ""}
            onChange={(e) => update("invoiceReference", e.target.value || undefined)}
            placeholder="INV-00123"
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
        </div>

        {/* Meeting details (virtual) */}
        {isVirtual && (
          <div className="bg-blue-50 rounded-lg p-4 space-y-3">
            <p className="text-xs font-semibold text-blue-800 uppercase tracking-wide">Meeting Details</p>
            <div>
              <label className="block text-xs font-medium text-blue-700 mb-1">Zoom/Teams URL</label>
              <input
                type="url"
                value={form.meetingUrl ?? ""}
                onChange={(e) => update("meetingUrl", e.target.value || undefined)}
                placeholder="https://zoom.us/j/..."
                className="w-full border border-blue-200 rounded-lg px-3 py-2 text-sm bg-white"
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-blue-700 mb-1">Meeting ID</label>
                <input
                  type="text"
                  value={form.meetingId ?? ""}
                  onChange={(e) => update("meetingId", e.target.value || undefined)}
                  placeholder="123 456 7890"
                  className="w-full border border-blue-200 rounded-lg px-3 py-2 text-sm bg-white"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-blue-700 mb-1">Passcode</label>
                <input
                  type="text"
                  value={form.meetingPasscode ?? ""}
                  onChange={(e) => update("meetingPasscode", e.target.value || undefined)}
                  placeholder="abc123"
                  className="w-full border border-blue-200 rounded-lg px-3 py-2 text-sm bg-white"
                />
              </div>
            </div>
          </div>
        )}

        {/* Venue (in-person) */}
        {!isVirtual && (
          <div className="bg-amber-50 rounded-lg p-4 space-y-3">
            <p className="text-xs font-semibold text-amber-800 uppercase tracking-wide">Venue Details</p>
            <div>
              <label className="block text-xs font-medium text-amber-700 mb-1">Venue Address</label>
              <textarea
                value={form.venueAddress ?? ""}
                onChange={(e) => update("venueAddress", e.target.value || undefined)}
                placeholder="Conference Room 3, 10 Downing Street, London SW1A 2AA"
                rows={2}
                className="w-full border border-amber-200 rounded-lg px-3 py-2 text-sm bg-white"
              />
            </div>
          </div>
        )}

        {/* Notes */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Notes</label>
          <textarea
            value={form.notes ?? ""}
            onChange={(e) => update("notes", e.target.value || undefined)}
            placeholder="Any additional details..."
            rows={2}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
        </div>

        {/* Actions */}
        <div className="flex gap-3 pt-4 border-t border-gray-200">
          <Button type="submit" disabled={saving}>
            {saving ? "Saving..." : "Save Changes"}
          </Button>
          <Button variant="secondary" type="button" onClick={onClose}>Cancel</Button>
        </div>
      </form>
    </SlideOver>
  );
}
