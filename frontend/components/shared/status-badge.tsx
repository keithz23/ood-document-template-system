import { Badge } from "@/components/ui/badge";
import type { DocumentStatus } from "@/types/api";

export function StatusBadge({ status }: { status: DocumentStatus }) {
  return (
    <Badge
      variant="outline"
      className={
        status === "Finalized"
          ? "border-emerald-200 bg-emerald-50 text-emerald-800"
          : "border-slate-200 bg-slate-100 text-slate-700"
      }
    >
      {status}
    </Badge>
  );
}
