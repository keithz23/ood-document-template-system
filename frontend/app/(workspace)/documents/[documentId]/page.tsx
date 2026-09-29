import { notFound } from "next/navigation";
import { DocumentEditor } from "@/features/documents/components/document-editor";
import { documents, getDocument, getTemplate } from "@/lib/mock-data";

export function generateStaticParams() { return documents.map((document) => ({ documentId: document.id })); }

export default async function DocumentPage({ params }: { params: Promise<{ documentId: string }> }) {
  const { documentId } = await params;
  const document = getDocument(documentId);
  const template = document ? getTemplate(document.templateId) : undefined;
  if (!document || !template) notFound();
  return <DocumentEditor template={template} document={document} />;
}
