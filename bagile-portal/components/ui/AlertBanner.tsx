import { AlertTriangle, Info, CheckCircle, XCircle } from "lucide-react";

const variants = {
  info: { bg: "bg-blue-50 border-blue-200 text-blue-800", icon: Info },
  warning: { bg: "bg-amber-50 border-amber-200 text-amber-800", icon: AlertTriangle },
  success: { bg: "bg-green-50 border-green-200 text-green-800", icon: CheckCircle },
  danger: { bg: "bg-red-50 border-red-200 text-red-800", icon: XCircle },
} as const;

interface AlertBannerProps {
  variant?: keyof typeof variants;
  children: React.ReactNode;
  onDismiss?: () => void;
}

export function AlertBanner({ variant = "info", children, onDismiss }: AlertBannerProps) {
  const { bg, icon: Icon } = variants[variant];
  return (
    <div className={`flex items-center gap-3 rounded-lg border px-4 py-3 text-sm ${bg}`}>
      <Icon className="w-4 h-4 shrink-0" />
      <div className="flex-1">{children}</div>
      {onDismiss && (
        <button onClick={onDismiss} className="text-current opacity-50 hover:opacity-100">
          <XCircle className="w-4 h-4" />
        </button>
      )}
    </div>
  );
}
