"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/features/auth/auth-provider";

export function AuthGuard({ children }: { children: ReactNode }) {
  const { session, isReady } = useAuth();
  const pathname = usePathname();
  const router = useRouter();

  useEffect(() => {
    if (isReady && !session) {
      const returnTo = encodeURIComponent(pathname || "/templates");
      router.replace(`/login?returnTo=${returnTo}`);
    }
  }, [isReady, pathname, router, session]);

  if (!isReady || !session) {
    return (
      <div aria-busy="true" aria-label="Checking your session" className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-4 w-full max-w-xl" />
        <Skeleton className="h-64 w-full rounded-xl" />
      </div>
    );
  }

  return children;
}
