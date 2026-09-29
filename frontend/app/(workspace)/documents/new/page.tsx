import { notFound } from "next/navigation";
import { DocumentEditor } from "@/features/documents/components/document-editor";
import { getTemplate, templates } from "@/lib/mock-data";

export default async function NewDocumentPage({ searchParams }: { searchParams: Promise<{ template?: string }> }) {
  const { template: templateId } = await searchParams;
  const template = getTemplate(templateId ?? templates[0].id);
  if (!template) notFound();
  return <DocumentEditor template={template} />;
}
