import { redirect } from "next/navigation";

interface Props {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>;
}

/**
 * /courses is retired. Redirect to /courseschedule?view=list, preserving query params.
 * /courses/[id] detail pages are untouched.
 */
export default async function CoursesPage({ searchParams }: Props) {
  const sp = await searchParams;
  const params = new URLSearchParams();
  params.set("view", "list");
  for (const [k, v] of Object.entries(sp)) {
    if (v !== undefined && k !== "view") {
      params.set(k, Array.isArray(v) ? v[0] : v);
    }
  }
  redirect(`/courseschedule?${params.toString()}`);
}
