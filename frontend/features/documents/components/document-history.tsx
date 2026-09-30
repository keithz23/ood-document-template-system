"use client";

import Link from "next/link";
import { ArrowUpRight, FileText, Search } from "lucide-react";
import { useMemo, useState } from "react";
import { EmptyState } from "@/components/shared/data-state";
import { StatusBadge } from "@/components/shared/status-badge";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";
import type { DocumentStatus, DocumentSummaryDto } from "@/types/api";

type Filter = "All" | DocumentStatus;
const filters: readonly Filter[] = ["All", "Draft", "Finalized"];

const dateFormatter = new Intl.DateTimeFormat("en", {
  year: "numeric",
  month: "short",
  day: "numeric",
});

export function DocumentHistory({ documents }: { documents: readonly DocumentSummaryDto[] }) {
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState<Filter>("All");
  const visibleDocuments = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase();
    return documents.filter((document) => {
      const matchesFilter = filter === "All" || document.status === filter;
      const matchesQuery = normalizedQuery.length === 0 || document.title.toLocaleLowerCase().includes(normalizedQuery) || document.source.templateName.toLocaleLowerCase().includes(normalizedQuery);
      return matchesFilter && matchesQuery;
    });
  }, [documents, filter, query]);

  return (
    <div className="space-y-5">
      <div className="flex flex-col gap-3 rounded-xl border bg-card p-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-wrap gap-1" role="group" aria-label="Filter documents by status">
          {filters.map((option) => (
            <button
              key={option}
              type="button"
              onClick={() => setFilter(option)}
              aria-pressed={filter === option}
              className={cn("min-h-9 rounded-lg px-3 text-sm font-medium outline-none transition-colors focus-visible:ring-3 focus-visible:ring-ring/40", filter === option ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-muted hover:text-foreground")}
            >
              {option}
            </button>
          ))}
        </div>
        <label className="relative block w-full lg:max-w-sm">
          <span className="sr-only">Search documents</span>
          <Search aria-hidden="true" className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search documents" className="h-10 pl-9" />
        </label>
      </div>

      <p aria-live="polite" className="text-sm text-muted-foreground">{visibleDocuments.length} {visibleDocuments.length === 1 ? "document" : "documents"}</p>

      {visibleDocuments.length === 0 ? (
        <EmptyState title="No documents match these filters" description="Try another status or search term." />
      ) : (
        <>
          <div className="hidden overflow-hidden rounded-xl border bg-card md:block">
            <table className="w-full border-collapse text-left text-sm">
              <caption className="sr-only">Your saved and finalized documents</caption>
              <thead className="border-b bg-muted/60 text-xs font-medium uppercase tracking-[0.04em] text-muted-foreground">
                <tr><th className="px-5 py-3 font-medium">Document</th><th className="px-4 py-3 font-medium">Template</th><th className="px-4 py-3 font-medium">Status</th><th className="px-4 py-3 font-medium">Last updated</th><th className="w-14 px-4 py-3"><span className="sr-only">Open</span></th></tr>
              </thead>
              <tbody className="divide-y">
                {visibleDocuments.map((document) => (
                  <tr key={document.id} className="group hover:bg-muted/35">
                    <td className="px-5 py-4"><Link href={`/documents/${document.id}`} className="font-medium outline-none group-hover:underline group-hover:underline-offset-4 focus-visible:ring-3 focus-visible:ring-ring/40">{document.title}</Link><p className="mt-1 text-xs text-muted-foreground">Created {dateFormatter.format(new Date(document.createdAt))}</p></td>
                    <td className="px-4 py-4 text-muted-foreground">{document.source.templateName}<span className="block text-xs">Version {document.source.versionNumber}</span></td>
                    <td className="px-4 py-4"><StatusBadge status={document.status} /></td>
                    <td className="px-4 py-4 tabular-nums text-muted-foreground">{dateFormatter.format(new Date(document.updatedAt))}</td>
                    <td className="px-4 py-4"><Link href={`/documents/${document.id}`} aria-label={`Open ${document.title}`} className="flex size-9 items-center justify-center rounded-lg text-muted-foreground outline-none hover:bg-muted hover:text-foreground focus-visible:ring-3 focus-visible:ring-ring/40"><ArrowUpRight aria-hidden="true" className="size-4" /></Link></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="space-y-3 md:hidden">
            {visibleDocuments.map((document) => (
              <article key={document.id} className="rounded-xl border bg-card p-4">
                <div className="flex items-start gap-3"><span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground"><FileText aria-hidden="true" className="size-4" /></span><div className="min-w-0 flex-1"><Link href={`/documents/${document.id}`} className="break-words font-medium leading-6 underline-offset-4 hover:underline">{document.title}</Link><p className="mt-1 text-sm text-muted-foreground">{document.source.templateName} · v{document.source.versionNumber}</p></div><StatusBadge status={document.status} /></div>
                <div className="mt-4 flex items-center justify-between border-t pt-3 text-xs text-muted-foreground"><span>Updated {dateFormatter.format(new Date(document.updatedAt))}</span><Link href={`/documents/${document.id}`} className="inline-flex min-h-9 items-center gap-1 rounded-md px-2 font-medium text-foreground outline-none focus-visible:ring-3 focus-visible:ring-ring/40">Open<ArrowUpRight aria-hidden="true" className="size-3.5" /></Link></div>
              </article>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
