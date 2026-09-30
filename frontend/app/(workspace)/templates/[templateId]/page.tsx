import { TemplateDetailScreen } from "@/features/templates/components/template-detail-screen";

export default async function TemplateDetailPage({ params }: { params: Promise<{ templateId: string }> }) {
  const { templateId } = await params;
  return <TemplateDetailScreen templateId={templateId} />;
}
