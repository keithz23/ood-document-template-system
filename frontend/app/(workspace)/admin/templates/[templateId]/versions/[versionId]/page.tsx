import { AdminTemplateVersionScreen } from "@/features/admin/components/admin-template-version-screen";

export default async function AdminTemplateVersionPage({
  params,
}: {
  params: Promise<{ templateId: string; versionId: string }>;
}) {
  const { templateId, versionId } = await params;
  return (
    <AdminTemplateVersionScreen
      templateId={templateId}
      versionId={versionId}
    />
  );
}
