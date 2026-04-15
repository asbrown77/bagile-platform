"use client";

import { PageHeader } from "@/components/ui/PageHeader";
import { CourseDefsEditor } from "@/components/courses/CourseDefsEditor";

export default function CourseTypesPage() {
  return (
    <div className="p-6 max-w-5xl mx-auto">
      <PageHeader title="Course Types" />
      <CourseDefsEditor />
    </div>
  );
}
