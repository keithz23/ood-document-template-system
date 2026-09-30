"use client";

import Link from "next/link";
import { ArrowLeft, CalendarDays, FileInput, Tags } from "lucide-react";
import {
  EmptyState,
  ErrorState,
  PageLoadingState,
} from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import { buttonVariants } from "@/components/ui/button";
import {
  useCurrentTemplateVersion,
  useTemplateDetail,
  useTemplateVersionPlaceholders,
} from "@/features/templates/api/template-queries";
import { TemplateDocumentPreview } from "@/features/templates/components/template-document-preview";
import { cn } from "@/lib/utils";
import { getApiErrorMessage } from "@/services/api-errors";

const dateFormatter = new Intl.DateTimeFormat("en", {
  year: "numeric",
  month: "short",
  day: "numeric",
});

export function TemplateDetailScreen({ templateId }: { templateId: string }) {
  const detailQuery = useTemplateDetail(templateId);
  const versionQuery = useCurrentTemplateVersion(templateId);
  const placeholdersQuery = useTemplateVersionPlaceholders(
    versionQuery.data?.id,
  );

  if (detailQuery.isPending || versionQuery.isPending) {
    return <PageLoadingState label="Loading template details" />;
  }

  if (detailQuery.isError || versionQuery.isError) {
    const error = detailQuery.error ?? versionQuery.error;
    return (
      <ErrorState
        title="Template details are unavailable"
        description={getApiErrorMessage(
          error,
          "We couldn’t load this template. Check your connection and try again.",
        )}
        onRetry={() => {
          void detailQuery.refetch();
          void versionQuery.refetch();
        }}
      />
    );
  }

  const template = detailQuery.data;
  const version = versionQuery.data;

  return (
    <div className="space-y-7">
      <Link
        href="/templates"
        className="inline-flex min-h-9 items-center gap-2 rounded-md text-sm font-medium text-muted-foreground outline-none hover:text-foreground focus-visible:ring-3 focus-visible:ring-ring/40"
      >
        <ArrowLeft aria-hidden="true" className="size-4" />
        Back to templates
      </Link>
      <PageHeader
        title={template.name}
        actions={
          <Link
            href={`/documents/new?template=${template.id}`}
            className={cn(buttonVariants(), "min-h-9 px-3")}
          >
            <FileInput aria-hidden="true" className="size-4" />
            Use this template
          </Link>
        }
      />
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <section aria-labelledby="template-preview-heading">
          <h2
            id="template-preview-heading"
            className="mb-3 text-base font-semibold"
          >
            Template preview
          </h2>
          <TemplateDocumentPreview
            content={version.content}
            title={template.name}
          />
        </section>
        <aside className="space-y-6">
          <section className="rounded-xl border bg-card p-5">
            <h2 className="text-base font-semibold">Template details</h2>
            <dl className="mt-4 space-y-4 text-sm">
              <div className="flex items-start gap-3">
                <Tags
                  aria-hidden="true"
                  className="mt-0.5 size-4 text-muted-foreground"
                />
                <div>
                  <dt className="text-muted-foreground">Category</dt>
                  <dd className="mt-0.5 font-medium">
                    {template.category.name}
                  </dd>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <FileInput
                  aria-hidden="true"
                  className="mt-0.5 size-4 text-muted-foreground"
                />
                <div>
                  <dt className="text-muted-foreground">Current version</dt>
                  <dd className="mt-0.5 font-medium">
                    Version {version.versionNumber}
                  </dd>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <CalendarDays
                  aria-hidden="true"
                  className="mt-0.5 size-4 text-muted-foreground"
                />
                <div>
                  <dt className="text-muted-foreground">Updated</dt>
                  <dd className="mt-0.5 font-medium">
                    {dateFormatter.format(new Date(version.updatedAt))}
                  </dd>
                </div>
              </div>
            </dl>
          </section>

          <section className="rounded-xl border bg-card p-5">
            <div className="flex items-baseline justify-between gap-4">
              <h2 className="text-base font-semibold">Document fields</h2>
              <span className="text-xs text-muted-foreground">
                {template.currentVersion.placeholderCount} total
              </span>
            </div>
            {placeholdersQuery.isPending ? (
              <div aria-busy="true" className="mt-4 space-y-3">
                <div className="h-10 animate-pulse rounded-md bg-muted" />
                <div className="h-10 animate-pulse rounded-md bg-muted" />
                <div className="h-10 animate-pulse rounded-md bg-muted" />
              </div>
            ) : null}
            {placeholdersQuery.isError ? (
              <div className="mt-4">
                <ErrorState
                  title="Fields are unavailable"
                  description={getApiErrorMessage(
                    placeholdersQuery.error,
                    "We couldn’t load the fields for this version.",
                  )}
                  onRetry={() => placeholdersQuery.refetch()}
                />
              </div>
            ) : null}
            {placeholdersQuery.data?.length === 0 ? (
              <div className="mt-4">
                <EmptyState
                  title="No fields required"
                  description="This template can be used without entering placeholder values."
                />
              </div>
            ) : null}
            {placeholdersQuery.data && placeholdersQuery.data.length > 0 ? (
              <ul className="mt-4 divide-y">
                {placeholdersQuery.data.map((placeholder) => (
                  <li
                    key={placeholder.id}
                    className="py-3 first:pt-0 last:pb-0"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <span className="text-sm font-medium">
                        {placeholder.label}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        {placeholder.dataType}
                      </span>
                    </div>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {placeholder.isRequired ? "Required" : "Optional"}
                    </p>
                  </li>
                ))}
              </ul>
            ) : null}
          </section>
        </aside>
      </div>
    </div>
  );
}
