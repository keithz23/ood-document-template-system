import Link from "next/link";
import { Plus } from "lucide-react";
import { EmptyState, ErrorState, PageLoadingState } from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import { buttonVariants } from "@/components/ui/button";
import { DocumentHistory } from "@/features/documents/components/document-history";
import { documents } from "@/lib/mock-data";
import { cn } from "@/lib/utils";

export default async function DocumentsPage({ searchParams }: { searchParams: Promise<{ state?: string }> }) {
  const { state } = await searchParams;
  return (
    <div className="space-y-7">
      <PageHeader title="My documents" description="Continue drafts or review finalized documents. Finalized documents remain read-only." actions={<Link href="/templates" className={cn(buttonVariants(), "min-h-9 px-3")}><Plus aria-hidden="true" className="size-4" />Create document</Link>} />
      {state === "loading" ? <PageLoadingState label="Loading document history" /> : null}
      {state === "error" ? <ErrorState title="Document history is unavailable" description="We couldn’t display your documents. Clear the simulated error and try again." /> : null}
      {state === "empty" ? <EmptyState title="No documents yet" description="Choose a template to create your first document." actionHref="/templates" actionLabel="Browse templates" /> : null}
      {!state ? <DocumentHistory documents={documents} /> : null}
    </div>
  );
}
