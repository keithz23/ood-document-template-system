import { AdminUserDetailScreen } from "@/features/admin/components/admin-user-detail-screen";

export default async function AdminUserDetailPage({
  params,
}: {
  params: Promise<{ userId: string }>;
}) {
  const { userId } = await params;
  return <AdminUserDetailScreen userId={userId} />;
}
