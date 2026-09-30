import { DocumentDetailScreen } from "@/features/documents/components/document-detail-screen";

export default async function DocumentPage({ params }: { params: Promise<{ documentId: string }> }) {
  const { documentId } = await params;
  return <DocumentDetailScreen documentId={documentId} />;
}
