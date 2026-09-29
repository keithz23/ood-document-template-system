import type { ReactNode } from "react";
import { AppShell } from "@/components/shared/app-shell";

export default function WorkspaceLayout({ children }: { children: ReactNode }) {
  return <AppShell>{children}</AppShell>;
}
