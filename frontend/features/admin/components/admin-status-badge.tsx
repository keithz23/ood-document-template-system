import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

export function AdminStatusBadge({
  label,
  tone,
}: {
  label: string;
  tone: "positive" | "warning" | "neutral";
}) {
  return (
    <Badge
      variant="outline"
      className={cn(
        "font-medium",
        tone === "positive" &&
          "border-emerald-200 bg-emerald-50 text-emerald-800",
        tone === "warning" && "border-amber-200 bg-amber-50 text-amber-900",
        tone === "neutral" && "border-slate-200 bg-slate-100 text-slate-700",
      )}
    >
      {label}
    </Badge>
  );
}
