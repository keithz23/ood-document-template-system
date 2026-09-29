import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, CalendarDays, FileInput, Tags } from "lucide-react";
import { PageHeader } from "@/components/shared/page-header";
import { buttonVariants } from "@/components/ui/button";
import { TemplateDocumentPreview } from "@/features/templates/components/template-document-preview";
import { getTemplate, templates } from "@/lib/mock-data";
import { cn } from "@/lib/utils";

export function generateStaticParams() { return templates.map((template) => ({ templateId: template.id })); }

export default async function TemplateDetailPage({ params }: { params: Promise<{ templateId: string }> }) {
  const { templateId } = await params;
  const template = getTemplate(templateId);
  if (!template) notFound();
  return (
    <div className="space-y-7">
      <Link href="/templates" className="inline-flex min-h-9 items-center gap-2 rounded-md text-sm font-medium text-muted-foreground outline-none hover:text-foreground focus-visible:ring-3 focus-visible:ring-ring/40"><ArrowLeft aria-hidden="true" className="size-4" />Back to templates</Link>
      <PageHeader title={template.name} description={template.description} actions={<Link href={`/documents/new?template=${template.id}`} className={cn(buttonVariants(), "min-h-9 px-3")}><FileInput aria-hidden="true" className="size-4" />Use this template</Link>} />
      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <section aria-labelledby="template-preview-heading"><h2 id="template-preview-heading" className="mb-3 text-base font-semibold">Template preview</h2><TemplateDocumentPreview template={template} /></section>
        <aside className="space-y-6">
          <section className="rounded-xl border bg-card p-5"><h2 className="text-base font-semibold">Template details</h2><dl className="mt-4 space-y-4 text-sm">
            <div className="flex items-start gap-3"><Tags aria-hidden="true" className="mt-0.5 size-4 text-muted-foreground" /><div><dt className="text-muted-foreground">Category</dt><dd className="mt-0.5 font-medium">{template.category}</dd></div></div>
            <div className="flex items-start gap-3"><FileInput aria-hidden="true" className="mt-0.5 size-4 text-muted-foreground" /><div><dt className="text-muted-foreground">Current version</dt><dd className="mt-0.5 font-medium">Version {template.versionNumber}</dd></div></div>
            <div className="flex items-start gap-3"><CalendarDays aria-hidden="true" className="mt-0.5 size-4 text-muted-foreground" /><div><dt className="text-muted-foreground">Updated</dt><dd className="mt-0.5 font-medium">{template.updatedAt}</dd></div></div>
          </dl></section>
          <section className="rounded-xl border bg-card p-5"><div className="flex items-baseline justify-between gap-4"><h2 className="text-base font-semibold">Document fields</h2><span className="text-xs text-muted-foreground">{template.placeholderCount} total</span></div><ul className="mt-4 divide-y">{template.placeholders.map((placeholder) => <li key={placeholder.key} className="py-3 first:pt-0 last:pb-0"><div className="flex items-start justify-between gap-3"><span className="text-sm font-medium">{placeholder.label}</span><span className="text-xs text-muted-foreground">{placeholder.dataType}</span></div><p className="mt-1 text-xs text-muted-foreground">{placeholder.isRequired ? "Required" : "Optional"}</p></li>)}</ul></section>
        </aside>
      </div>
    </div>
  );
}
