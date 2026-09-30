import { AdminTemplateDetailScreen } from "@/features/admin/components/admin-template-detail-screen";

export default async function AdminTemplateDetailPage({
  params,
}: {
  params: Promise<{ templateId: string }>;
}) {
  const { templateId } = await params;
  return <AdminTemplateDetailScreen templateId={templateId} />;
}
