"use client";

import Link from "next/link";
import { ArrowRight, FileText, Search } from "lucide-react";
import { useMemo, useState } from "react";
import { EmptyState } from "@/components/shared/data-state";
import { buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import type { TemplateGalleryItemDto } from "@/types/api";

export function TemplateGallery({
  templates,
}: {
  templates: readonly TemplateGalleryItemDto[];
}) {
  const [query, setQuery] = useState("");
  const [categoryId, setCategoryId] = useState("all");
  const categories = useMemo(
    () =>
      Array.from(
        new Map(
          templates.map((template) => [
            template.category.id,
            template.category,
          ]),
        ).values(),
      ).sort((left, right) => left.name.localeCompare(right.name)),
    [templates],
  );
  const visibleTemplates = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase();
    return templates.filter((template) => {
      const matchesCategory =
        categoryId === "all" || template.category.id === categoryId;
      const matchesQuery =
        normalizedQuery.length === 0 ||
        template.name.toLocaleLowerCase().includes(normalizedQuery);
      return matchesCategory && matchesQuery;
    });
  }, [categoryId, query, templates]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 rounded-xl border bg-card p-4 sm:flex-row sm:items-center">
        <label className="relative block flex-1">
          <span className="sr-only">Search templates</span>
          <Search
            aria-hidden="true"
            className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
          />
          <Input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search by template name"
            className="h-10 pl-9"
          />
        </label>
        <label className="sm:w-52">
          <span className="sr-only">Filter by category</span>
          <select
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/40"
          >
            <option value="all">All templates</option>
            {categories.map((option) => (
              <option key={option.id} value={option.id}>
                {option.name}
              </option>
            ))}
          </select>
        </label>
      </div>
      <p aria-live="polite" className="text-sm text-muted-foreground">
        {visibleTemplates.length}{" "}
        {visibleTemplates.length === 1 ? "template" : "templates"}
      </p>
      {visibleTemplates.length === 0 ? (
        <EmptyState
          title="No templates match these filters"
          description="Try a different search term or choose another category."
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {visibleTemplates.map((template) => (
            <article
              key={template.id}
              className="group flex min-h-72 flex-col rounded-xl border bg-card p-5 transition-colors hover:border-muted-foreground/35"
            >
              <div className="flex items-start justify-between gap-4">
                <span className="flex size-10 items-center justify-center rounded-lg bg-muted text-muted-foreground">
                  <FileText aria-hidden="true" className="size-5" />
                </span>
                <span className="rounded-md bg-muted px-2 py-1 text-xs font-medium text-muted-foreground">
                  {template.category.name}
                </span>
              </div>
              <h2 className="mt-5 text-base font-semibold leading-6 tracking-[-0.01em]">
                <Link
                  href={`/templates/${template.id}`}
                  className="rounded-sm outline-none focus-visible:ring-3 focus-visible:ring-ring/40"
                >
                  {template.name}
                </Link>
              </h2>
              <dl className="mt-auto flex items-center gap-3 pt-6 text-xs text-muted-foreground">
                <div className="flex gap-1">
                  <dt>Version</dt>
                  <dd className="font-medium text-foreground">
                    {template.currentVersion.versionNumber}
                  </dd>
                </div>
                <span aria-hidden="true">·</span>
                <div>
                  <dt className="sr-only">Placeholder count</dt>
                  <dd>{template.currentVersion.placeholderCount} fields</dd>
                </div>
              </dl>
              <Link
                href={`/templates/${template.id}`}
                className={cn(
                  buttonVariants({ variant: "outline" }),
                  "mt-4 min-h-9 w-full justify-between px-3",
                )}
              >
                View template
                <ArrowRight
                  aria-hidden="true"
                  className="size-4 transition-transform group-hover:translate-x-0.5"
                />
              </Link>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}
