"use client";

import { useEffect, useState } from "react";
import {
  CourseContact,
  AddCourseContactPayload,
  getCourseContacts,
  addCourseContact,
  deleteCourseContact,
  updateCourseContact,
} from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { UserPlus, Trash2, Phone, Mail, Pencil, Check, X } from "lucide-react";

interface Props {
  apiKey: string;
  courseId: number;
}

const ROLE_LABELS: Record<string, string> = {
  admin: "Admin",
  organiser: "Organiser",
  other: "Other",
};

const ROLE_COLOURS: Record<string, string> = {
  admin: "bg-purple-100 text-purple-700",
  organiser: "bg-blue-100 text-blue-700",
  other: "bg-gray-100 text-gray-600",
};

interface EditState {
  role: string;
  name: string;
  email: string;
  phone: string;
}

export function CourseContactsSection({ apiKey, courseId }: Props) {
  const [contacts, setContacts] = useState<CourseContact[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [deletingId, setDeletingId] = useState<number | null>(null);

  // Add form state
  const [form, setForm] = useState<AddCourseContactPayload>({
    role: "organiser",
    name: "",
    email: "",
    phone: "",
  });
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");

  // Edit state — keyed by contact id, null means not editing
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editForm, setEditForm] = useState<EditState>({ role: "organiser", name: "", email: "", phone: "" });
  const [editSaving, setEditSaving] = useState(false);
  const [editError, setEditError] = useState("");

  useEffect(() => {
    loadContacts();
  }, [courseId]);

  async function loadContacts() {
    setLoading(true);
    try {
      const data = await getCourseContacts(apiKey, courseId);
      setContacts(data);
    } catch {
      setError("Failed to load contacts");
    } finally {
      setLoading(false);
    }
  }

  async function handleAdd() {
    if (!form.name.trim() || !form.email.trim()) {
      setFormError("Name and email are required");
      return;
    }
    setSaving(true);
    setFormError("");
    try {
      const payload: AddCourseContactPayload = {
        role: form.role,
        name: form.name.trim(),
        email: form.email.trim(),
        phone: form.phone?.trim() || undefined,
      };
      await addCourseContact(apiKey, courseId, payload);
      setForm({ role: "organiser", name: "", email: "", phone: "" });
      setShowForm(false);
      await loadContacts();
    } catch {
      setFormError("Failed to add contact");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(contactId: number, name: string) {
    if (!confirm(`Remove ${name} from this course?`)) return;
    setDeletingId(contactId);
    try {
      await deleteCourseContact(apiKey, courseId, contactId);
      setContacts((prev) => prev.filter((c) => c.id !== contactId));
    } catch {
      setError("Failed to remove contact");
    } finally {
      setDeletingId(null);
    }
  }

  function startEdit(contact: CourseContact) {
    setEditingId(contact.id);
    setEditForm({
      role: contact.role,
      name: contact.name,
      email: contact.email,
      phone: contact.phone ?? "",
    });
    setEditError("");
  }

  function cancelEdit() {
    setEditingId(null);
    setEditError("");
  }

  async function handleSaveEdit(contactId: number) {
    if (!editForm.name.trim() || !editForm.email.trim()) {
      setEditError("Name and email are required");
      return;
    }
    setEditSaving(true);
    setEditError("");
    try {
      const updated = await updateCourseContact(apiKey, courseId, contactId, {
        role: editForm.role,
        name: editForm.name.trim(),
        email: editForm.email.trim(),
        phone: editForm.phone.trim() || undefined,
      });
      setContacts((prev) => prev.map((c) => (c.id === contactId ? updated : c)));
      setEditingId(null);
    } catch {
      setEditError("Failed to save changes");
    } finally {
      setEditSaving(false);
    }
  }

  const inputCls =
    "w-full border border-gray-300 rounded-lg px-2.5 py-1 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500";
  const selectCls =
    "w-full border border-gray-300 rounded-lg px-2.5 py-1 text-sm bg-white focus:ring-2 focus:ring-brand-500 focus:border-brand-500";

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden mt-6">
      {/* Header */}
      <div className="px-5 py-3 border-b border-gray-200 flex items-center justify-between">
        <div>
          <h3 className="text-sm font-semibold text-gray-900">Course Contacts</h3>
          <p className="text-xs text-gray-500 mt-0.5">Logistics and admin contacts for this course</p>
        </div>
        {!showForm && (
          <Button variant="secondary" size="sm" onClick={() => setShowForm(true)}>
            <UserPlus className="w-3.5 h-3.5" /> Add Contact
          </Button>
        )}
      </div>

      {error && (
        <div className="px-5 pt-3">
          <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>
        </div>
      )}

      {/* Add form */}
      {showForm && (
        <div className="px-5 py-4 border-b border-gray-100 bg-gray-50 space-y-3">
          <p className="text-xs font-medium text-gray-700">Add contact</p>
          {formError && (
            <AlertBanner variant="danger" onDismiss={() => setFormError("")}>{formError}</AlertBanner>
          )}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs text-gray-600 mb-1">Role</label>
              <select
                value={form.role}
                onChange={(e) => setForm({ ...form, role: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              >
                <option value="organiser">Organiser</option>
                <option value="admin">Admin</option>
                <option value="other">Other</option>
              </select>
            </div>
            <div>
              <label className="block text-xs text-gray-600 mb-1">Name *</label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                placeholder="e.g. Debbie Gooch"
                className="w-full border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-600 mb-1">Email *</label>
              <input
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                placeholder="email@example.com"
                className="w-full border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              />
            </div>
            <div>
              <label className="block text-xs text-gray-600 mb-1">Phone (optional)</label>
              <input
                type="tel"
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
                placeholder="+44 7xxx xxxxxx"
                className="w-full border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              />
            </div>
          </div>
          <div className="flex gap-2 pt-1">
            <Button size="sm" onClick={handleAdd} disabled={saving}>
              {saving ? "Adding..." : "Add Contact"}
            </Button>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => { setShowForm(false); setFormError(""); }}
            >
              Cancel
            </Button>
          </div>
        </div>
      )}

      {/* Contact list */}
      {loading ? (
        <div className="p-4 text-sm text-gray-400">Loading...</div>
      ) : contacts.length === 0 && !showForm ? (
        <div className="px-5 py-6 text-center text-sm text-gray-400">
          No contacts added yet. Use "Add Contact" to add an organiser or admin.
        </div>
      ) : (
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Name</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Role</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Contact</th>
              <th className="px-4 py-2" />
            </tr>
          </thead>
          <tbody>
            {contacts.map((c) =>
              editingId === c.id ? (
                /* ── Inline edit row ── */
                <tr key={c.id} className="border-t border-gray-100 bg-amber-50">
                  <td className="px-4 py-2" colSpan={4}>
                    {editError && (
                      <div className="mb-2">
                        <AlertBanner variant="danger" onDismiss={() => setEditError("")}>{editError}</AlertBanner>
                      </div>
                    )}
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-2 items-end">
                      <div>
                        <label className="block text-xs text-gray-500 mb-0.5">Name *</label>
                        <input
                          type="text"
                          value={editForm.name}
                          onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                          className={inputCls}
                          autoFocus
                        />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-500 mb-0.5">Email *</label>
                        <input
                          type="email"
                          value={editForm.email}
                          onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
                          className={inputCls}
                        />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-500 mb-0.5">Phone</label>
                        <input
                          type="tel"
                          value={editForm.phone}
                          onChange={(e) => setEditForm({ ...editForm, phone: e.target.value })}
                          placeholder="optional"
                          className={inputCls}
                        />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-500 mb-0.5">Role</label>
                        <select
                          value={editForm.role}
                          onChange={(e) => setEditForm({ ...editForm, role: e.target.value })}
                          className={selectCls}
                        >
                          <option value="organiser">Organiser</option>
                          <option value="admin">Admin</option>
                          <option value="other">Other</option>
                        </select>
                      </div>
                    </div>
                    <div className="flex gap-2 mt-2">
                      <button
                        onClick={() => handleSaveEdit(c.id)}
                        disabled={editSaving}
                        className="inline-flex items-center gap-1 text-xs font-medium px-2.5 py-1 rounded-md bg-brand-600 text-white hover:bg-brand-700 disabled:opacity-50"
                      >
                        <Check className="w-3 h-3" />
                        {editSaving ? "Saving..." : "Save"}
                      </button>
                      <button
                        onClick={cancelEdit}
                        disabled={editSaving}
                        className="inline-flex items-center gap-1 text-xs font-medium px-2.5 py-1 rounded-md bg-gray-100 text-gray-700 hover:bg-gray-200 disabled:opacity-50"
                      >
                        <X className="w-3 h-3" />
                        Cancel
                      </button>
                    </div>
                  </td>
                </tr>
              ) : (
                /* ── Normal read row ── */
                <tr key={c.id} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-2.5 font-medium text-gray-900">{c.name}</td>
                  <td className="px-4 py-2.5">
                    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${ROLE_COLOURS[c.role] || ROLE_COLOURS.other}`}>
                      {ROLE_LABELS[c.role] || c.role}
                    </span>
                  </td>
                  <td className="px-4 py-2.5 text-gray-600 hidden md:table-cell">
                    <a
                      href={`mailto:${c.email}`}
                      className="flex items-center gap-1 text-brand-600 hover:underline text-xs"
                    >
                      <Mail className="w-3 h-3" />{c.email}
                    </a>
                    {c.phone && (
                      <a
                        href={`tel:${c.phone}`}
                        className="flex items-center gap-1 text-gray-500 text-xs mt-0.5"
                      >
                        <Phone className="w-3 h-3" />{c.phone}
                      </a>
                    )}
                  </td>
                  <td className="px-4 py-2.5 text-right">
                    <div className="inline-flex items-center gap-2">
                      <button
                        onClick={() => startEdit(c)}
                        className="text-gray-400 hover:text-brand-600 inline-flex items-center"
                        title={`Edit ${c.name}`}
                      >
                        <Pencil className="w-3.5 h-3.5" />
                      </button>
                      <button
                        onClick={() => handleDelete(c.id, c.name)}
                        disabled={deletingId === c.id}
                        className="text-red-400 hover:text-red-600 disabled:opacity-40 inline-flex items-center"
                        title={`Remove ${c.name}`}
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              )
            )}
          </tbody>
        </table>
      )}
    </div>
  );
}
