"use client";

import Link from "next/link";
import { ArrowRight, FileText, Search } from "lucide-react";
import { useMemo, useState, type CSSProperties } from "react";
import { EmptyState } from "@/components/shared/data-state";
import { buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import type { TemplateGalleryItemDto } from "@/types/api";

const categoryHues = [268, 225, 170, 55, 300, 25];

function categoryHue(categoryId: string) {
  let hash = 0;
  for (const char of categoryId) hash = (hash * 31 + char.charCodeAt(0)) >>> 0;
  return categoryHues[hash % categoryHues.length];
}

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
      <div className="flex flex-col gap-3 rounded-xl border bg-card shadow-xs p-3 shadow-xs sm:flex-row sm:items-center">
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
              style={{ "--cat-h": categoryHue(template.category.id) } as CSSProperties}
              className="group flex flex-col overflow-hidden rounded-xl border bg-card shadow-xs transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/25 hover:shadow-lg hover:shadow-primary/5"
            >
              <div aria-hidden="true" className="cat-tint relative h-36 overflow-hidden">
                <div className="absolute inset-x-8 top-6 h-40 rounded-t-md bg-card p-4 shadow-md ring-1 ring-black/5 transition-transform duration-300 group-hover:-translate-y-1">
                  <div className="cat-solid h-2 w-1/2 rounded-full opacity-80" />
                  <div className="mt-3 space-y-1.5">
                    <div className="h-1.5 w-full rounded-full bg-muted" />
                    <div className="h-1.5 w-11/12 rounded-full bg-muted" />
                    <div className="h-1.5 w-4/5 rounded-full bg-muted" />
                  </div>
                  <div className="mt-3 flex gap-1.5">
                    <div className="cat-tint h-3 w-14 rounded-sm" />
                    <div className="cat-tint h-3 w-10 rounded-sm" />
                  </div>
                </div>
              </div>
              <div className="flex flex-1 flex-col p-5">
                <div className="flex items-center gap-2">
                  <span className="cat-tint inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-semibold">
                    <span className="cat-solid size-1.5 rounded-full" />
                    {template.category.name}
                  </span>
                </div>
                <h2 className="mt-3 text-base font-semibold leading-6 tracking-[-0.01em]">
                  <Link
                    href={`/templates/${template.id}`}
                    className="rounded-sm outline-none focus-visible:ring-3 focus-visible:ring-ring/40"
                  >
                    {template.name}
                  </Link>
                </h2>
                <dl className="mt-auto flex items-center gap-3 pt-4 text-xs text-muted-foreground">
                  <div className="flex items-center gap-1">
                    <FileText aria-hidden="true" className="size-3.5" />
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
                    "mt-4 min-h-9 w-full justify-between px-3 group-hover:border-primary/30 group-hover:text-primary",
                  )}
                >
                  View template
                  <ArrowRight
                    aria-hidden="true"
                    className="size-4 transition-transform group-hover:translate-x-0.5"
                  />
                </Link>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}
