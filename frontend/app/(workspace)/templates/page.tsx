import { ErrorState, EmptyState, PageLoadingState } from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import { TemplateGallery } from "@/features/templates/components/template-gallery";
import { templateSummaries } from "@/lib/mock-data";

export default async function TemplatesPage({ searchParams }: { searchParams: Promise<{ state?: string }> }) {
  const { state } = await searchParams;
  return <div className="space-y-7"><PageHeader title="Templates" description="Choose an active template to start a new document. You can review its fields and source version before you begin." />{state === "loading" ? <PageLoadingState label="Loading templates" /> : null}{state === "error" ? <ErrorState title="Templates are unavailable" description="We couldn’t display the template library. Clear the simulated error and try again." /> : null}{state === "empty" ? <EmptyState title="No active templates yet" description="Active templates will appear here when they are available." /> : null}{!state ? <TemplateGallery templates={templateSummaries} /> : null}</div>;
}
