import { Badge } from "@/components/ui/badge";
import type { DocumentStatus } from "@/types/api";

export function StatusBadge({ status }: { status: DocumentStatus }) {
  return (
    <Badge
      variant="outline"
      className={
        status === "Finalized"
          ? "border-success/25 bg-success-subtle text-success"
          : "border-border bg-muted text-muted-foreground"
      }
    >
      {status}
    </Badge>
  );
}
