"use client";

import Link from "next/link";
import { AlertCircle, FileQuestion, RotateCcw } from "lucide-react";
import { Button, buttonVariants } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

export function PageLoadingState({
  label = "Loading content",
}: {
  label?: string;
}) {
  return (
    <div aria-busy="true" aria-label={label} className="space-y-6">
      <div className="space-y-2">
        <Skeleton className="h-7 w-52" />
        <Skeleton className="h-4 w-full max-w-xl" />
      </div>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {Array.from({ length: 6 }, (_, index) => (
          <div key={index} className="rounded-xl border bg-card p-5">
            <Skeleton className="h-4 w-24" />
            <Skeleton className="mt-5 h-5 w-3/4" />
            <Skeleton className="mt-3 h-4 w-full" />
            <Skeleton className="mt-2 h-4 w-5/6" />
            <Skeleton className="mt-8 h-9 w-full" />
          </div>
        ))}
      </div>
    </div>
  );
}

type EmptyStateProps = Readonly<{
  title: string;
  description: string;
  actionHref?: string;
  actionLabel?: string;
}>;

export function EmptyState({
  title,
  description,
  actionHref,
  actionLabel,
}: EmptyStateProps) {
  return (
    <section className="flex min-h-80 flex-col items-center justify-center rounded-xl border border-dashed bg-card px-6 py-12 text-center">
      <div className="flex size-10 items-center justify-center rounded-lg bg-muted text-muted-foreground">
        <FileQuestion aria-hidden="true" className="size-5" />
      </div>
      <h2 className="mt-4 text-base font-semibold">{title}</h2>
      <p className="mt-1 max-w-md text-sm leading-6 text-muted-foreground">
        {description}
      </p>
      {actionHref && actionLabel ? (
        <Link
          href={actionHref}
          className={cn(buttonVariants(), "mt-5 min-h-9 px-3")}
        >
          {actionLabel}
        </Link>
      ) : null}
    </section>
  );
}

export function ErrorState({
  title = "We couldn’t load this page",
  description = "The requested content could not be displayed. Try loading it again.",
  onRetry,
}: Readonly<{ title?: string; description?: string; onRetry?: () => void }>) {
  return (
    <section
      role="alert"
      className="flex min-h-80 flex-col items-center justify-center rounded-xl border bg-card px-6 py-12 text-center"
    >
      <div className="flex size-10 items-center justify-center rounded-lg bg-red-50 text-red-700">
        <AlertCircle aria-hidden="true" className="size-5" />
      </div>
      <h2 className="mt-4 text-base font-semibold">{title}</h2>
      <p className="mt-1 max-w-md text-sm leading-6 text-muted-foreground">
        {description}
      </p>
      {onRetry ? (
        <Button
          type="button"
          variant="outline"
          className="mt-5"
          onClick={onRetry}
        >
          <RotateCcw aria-hidden="true" className="size-4" />
          Try again
        </Button>
      ) : (
        <Link
          href="?"
          className={cn(
            buttonVariants({ variant: "outline" }),
            "mt-5 min-h-9 px-3",
          )}
        >
          <RotateCcw aria-hidden="true" className="size-4" />
          Try again
        </Link>
      )}
    </section>
  );
}
