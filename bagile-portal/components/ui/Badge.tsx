const variants = {
  success: "bg-green-100 text-green-700",
  warning: "bg-amber-100 text-amber-700",
  danger: "bg-red-100 text-red-700",
  info: "bg-blue-100 text-blue-700",
  neutral: "bg-gray-100 text-gray-600",
} as const;

interface BadgeProps {
  children: React.ReactNode;
  variant?: keyof typeof variants;
  dot?: boolean;
}

export function Badge({ children, variant = "neutral", dot }: BadgeProps) {
  return (
    <span className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium ${variants[variant]}`}>
      {dot && <span className={`w-1.5 h-1.5 rounded-full ${
        variant === "success" ? "bg-green-500" :
        variant === "warning" ? "bg-amber-500" :
        variant === "danger" ? "bg-red-500" :
        variant === "info" ? "bg-blue-500" : "bg-gray-400"
      }`} />}
      {children}
    </span>
  );
}

export function statusBadge(status: string) {
  const map: Record<string, { label: string; variant: keyof typeof variants }> = {
    running: { label: "Running", variant: "info" },
    completed: { label: "Completed", variant: "neutral" },
    guaranteed: { label: "Guaranteed", variant: "success" },
    monitor: { label: "Monitor", variant: "warning" },
    cancel: { label: "Cancel", variant: "danger" },
    "at risk": { label: "At Risk", variant: "warning" },
    "at_risk": { label: "At Risk", variant: "warning" },
    active: { label: "Active", variant: "success" },
    publish: { label: "Published", variant: "info" },
    cancelled: { label: "Cancelled", variant: "neutral" },
    transferred: { label: "Transferred", variant: "info" },
    pending_transfer: { label: "Pending Transfer", variant: "warning" },
    refunded: { label: "Refunded", variant: "danger" },
    confirmed: { label: "Confirmed", variant: "success" },
    draft: { label: "Draft", variant: "neutral" },
    published: { label: "Published", variant: "info" },
  };
  const entry = map[status] || { label: status, variant: "neutral" as const };
  return <Badge variant={entry.variant} dot>{entry.label}</Badge>;
}
