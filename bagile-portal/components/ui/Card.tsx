import { TrendingUp, TrendingDown } from "lucide-react";

interface CardProps {
  label: string;
  value: string | number;
  subtitle?: string;
  trend?: { value: number; isPositive: boolean };
  variant?: "default" | "success" | "danger";
  icon?: React.ReactNode;
}

const variantStyles = {
  default: "bg-white border-gray-200",
  success: "bg-green-50 border-green-200",
  danger: "bg-red-50 border-red-200",
};

export function Card({ label, value, subtitle, trend, variant = "default", icon }: CardProps) {
  return (
    <div className={`rounded-xl border p-5 ${variantStyles[variant]}`}>
      <div className="flex items-center justify-between mb-1">
        <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">{label}</p>
        {icon && <span className="text-gray-400">{icon}</span>}
      </div>
      <p className={`text-2xl font-bold ${variant === "danger" ? "text-red-700" : variant === "success" ? "text-green-700" : "text-gray-900"}`}>
        {value}
      </p>
      <div className="flex items-center gap-2 mt-1">
        {subtitle && <p className="text-xs text-gray-500">{subtitle}</p>}
        {trend && (
          <span className={`inline-flex items-center gap-0.5 text-xs font-medium ${trend.isPositive ? "text-green-600" : "text-red-600"}`}>
            {trend.isPositive ? <TrendingUp className="w-3 h-3" /> : <TrendingDown className="w-3 h-3" />}
            {trend.isPositive ? "+" : ""}{trend.value}%
          </span>
        )}
      </div>
    </div>
  );
}
