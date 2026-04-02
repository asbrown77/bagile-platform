"use client";

import { SlideOver } from "@/components/ui/SlideOver";
import { CourseScheduleDetail } from "@/lib/api";
import { PrivateCourseForm } from "./PrivateCourseForm";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  course: CourseScheduleDetail;
  onSaved: () => void;
}

export function EditPrivateCoursePanel({ open, onClose, apiKey, course, onSaved }: Props) {
  return (
    <SlideOver open={open} onClose={onClose} title="Edit Course" subtitle={course.courseCode ?? undefined}>
      <PrivateCourseForm
        mode="edit"
        course={course}
        apiKey={apiKey}
        onSuccess={onSaved}
        onCancel={onClose}
      />
    </SlideOver>
  );
}
