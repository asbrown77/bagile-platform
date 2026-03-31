"use client";

import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Users } from "lucide-react";

export default function StudentsPage() {
  return (
    <>
      <PageHeader title="Students" subtitle="Search and manage students" />
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8">
        <EmptyState
          icon={<Users className="w-10 h-10" />}
          title="Coming soon"
          description="Student search and management is being built in the next sprint"
        />
      </div>
    </>
  );
}
