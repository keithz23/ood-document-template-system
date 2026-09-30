import type { ReactNode } from "react";
import { AppShell } from "@/components/shared/app-shell";
import { AuthGuard } from "@/features/auth/auth-guard";

export default function WorkspaceLayout({ children }: { children: ReactNode }) {
  return (
    <AppShell>
      <AuthGuard>{children}</AuthGuard>
    </AppShell>
  );
}
