"use client";

import Link from "next/link";
import { Plus } from "lucide-react";
import { EmptyState, ErrorState, PageLoadingState } from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import { buttonVariants } from "@/components/ui/button";
import { useDocumentHistory } from "@/features/documents/api/document-queries";
import { DocumentHistory } from "@/features/documents/components/document-history";
import { cn } from "@/lib/utils";
import { getApiErrorMessage } from "@/services/api-errors";

export function DocumentsScreen() {
  const historyQuery = useDocumentHistory();

  return (
    <div className="space-y-7">
      <PageHeader
        title="My documents"
        description="Continue Drafts or review Finalized documents. Finalized documents remain read-only."
        actions={<Link href="/templates" className={cn(buttonVariants(), "min-h-9 px-3")}><Plus aria-hidden="true" className="size-4" />Create document</Link>}
      />
      {historyQuery.isPending ? <PageLoadingState label="Loading document history" /> : null}
      {historyQuery.isError ? (
        <ErrorState
          title="Document history is unavailable"
          description={getApiErrorMessage(historyQuery.error, "We couldn’t display your documents. Check your connection and try again.")}
          onRetry={() => historyQuery.refetch()}
        />
      ) : null}
      {historyQuery.data?.length === 0 ? <EmptyState title="No documents yet" description="Choose a template to create your first document." actionHref="/templates" actionLabel="Browse templates" /> : null}
      {historyQuery.data && historyQuery.data.length > 0 ? <DocumentHistory documents={historyQuery.data} /> : null}
    </div>
  );
}
