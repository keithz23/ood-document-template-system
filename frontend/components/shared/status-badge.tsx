import { Badge } from "@/components/ui/badge";
import type { DocumentStatus } from "@/types/api";

export function StatusBadge({ status }: { status: DocumentStatus }) {
  return (
    <Badge
      variant="outline"
      className={
        status === "Finalized"
          ? "border-success/25 bg-success-subtle text-success"
          : "border-warning/20 bg-warning-subtle text-warning"
      }
    >
      <span
        aria-hidden="true"
        className={
          status === "Finalized"
            ? "size-1.5 rounded-full bg-success"
            : "size-1.5 rounded-full bg-warning"
        }
      />
      {status}
    </Badge>
  );
}
