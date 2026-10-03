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
          "border-success/25 bg-success-subtle text-success",
        tone === "warning" && "border-warning/25 bg-warning-subtle text-warning",
        tone === "neutral" && "border-border bg-muted text-muted-foreground",
      )}
    >
      {label}
    </Badge>
  );
}
