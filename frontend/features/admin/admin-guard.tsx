"use client";

import Link from "next/link";
import { ShieldAlert } from "lucide-react";
import type { ReactNode } from "react";
import { buttonVariants } from "@/components/ui/button";
import { useAuth } from "@/features/auth/auth-provider";
import { cn } from "@/lib/utils";

export function AdminGuard({ children }: { children: ReactNode }) {
  const { hasPermission } = useAuth();
  const canAccessAdministration =
    hasPermission("Templates.Manage") ||
    hasPermission("Users.View") ||
    hasPermission("AuditLogs.View");

  if (!canAccessAdministration) {
    return (
      <section className="flex min-h-80 flex-col items-center justify-center rounded-xl border bg-card px-6 py-12 text-center">
        <div className="flex size-10 items-center justify-center rounded-lg bg-amber-50 text-amber-800">
          <ShieldAlert aria-hidden="true" className="size-5" />
        </div>
        <h1 className="mt-4 text-lg font-semibold">Admin access required</h1>
        <p className="mt-1 max-w-md text-sm leading-6 text-muted-foreground">
          Your account does not have permission to access administration
          tools.
        </p>
        <Link
          href="/templates"
          className={cn(buttonVariants({ variant: "outline" }), "mt-5")}
        >
          Return to templates
        </Link>
      </section>
    );
  }

  return children;
}
