import { notFound } from "next/navigation";
import { NewDocumentScreen } from "@/features/documents/components/new-document-screen";

export default async function NewDocumentPage({ searchParams }: { searchParams: Promise<{ template?: string }> }) {
  const { template: templateId } = await searchParams;
  if (!templateId) notFound();
  return <NewDocumentScreen templateId={templateId} />;
}
