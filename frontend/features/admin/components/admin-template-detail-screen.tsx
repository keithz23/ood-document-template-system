"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, ArrowRight, CheckCircle2, LoaderCircle, Pencil, Plus } from "lucide-react";
import { useMemo, useState } from "react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  EmptyState,
  ErrorState,
  PageLoadingState,
} from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import {
  useAdminCategories,
  useAdminTemplate,
  useAdminTemplateVersions,
  useCreateDraftTemplateVersion,
  useSetTemplateActive,
  useUpdateDraftTemplate,
} from "@/features/admin/api/admin-queries";
import { TemplateFormDialog } from "@/features/admin/components/admin-form-dialogs";
import { AdminStatusBadge } from "@/features/admin/components/admin-status-badge";
import { cn } from "@/lib/utils";
import { getApiErrorMessage } from "@/services/api-errors";
import type { TemplateStatus } from "@/types/api";

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  }).format(new Date(value));
}

function statusTone(status: TemplateStatus) {
  if (status === "Active") return "positive" as const;
  if (status === "Draft") return "warning" as const;
  return "neutral" as const;
}

export function AdminTemplateDetailScreen({
  templateId,
}: {
  templateId: string;
}) {
  const templateQuery = useAdminTemplate(templateId);
  const versionsQuery = useAdminTemplateVersions(templateId);
  const categoriesQuery = useAdminCategories();
  const updateMutation = useUpdateDraftTemplate(templateId);
  const stateMutation = useSetTemplateActive(templateId);
  const createVersionMutation = useCreateDraftTemplateVersion(templateId);
  const router = useRouter();
  const [editOpen, setEditOpen] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const template = templateQuery.data;
  const versions = versionsQuery.data ?? template?.versions ?? [];
  const categories = categoriesQuery.data ?? [];
  const initialValues = useMemo(
    () => ({
      name: template?.name ?? "",
      categoryId: template?.category.id ?? "",
    }),
    [template?.category.id, template?.name],
  );

  if (templateQuery.isPending)
    return <PageLoadingState label="Loading template details" />;
  if (templateQuery.isError || !template) {
    return (
      <ErrorState
        title="Template details are unavailable"
        description={getApiErrorMessage(
          templateQuery.error,
          "We couldn’t load this template. Check your connection and try again.",
        )}
        onRetry={() => templateQuery.refetch()}
      />
    );
  }

  const hasCurrentPublishedVersion = versions.some(
    (version) => version.isCurrent && version.status === "Published",
  );
  const activationDisabled =
    template.status !== "Active" && !hasCurrentPublishedVersion;

  async function update(values: { name: string; categoryId: string }) {
    const result = await updateMutation.mutateAsync(values);
    setEditOpen(false);
    setSuccessMessage(`Template “${result.name}” was updated.`);
  }

  async function changeState(id: string, currentStatus: TemplateStatus) {
    const active = currentStatus !== "Active";
    const result = await stateMutation.mutateAsync({ id, active });
    setSuccessMessage(
      `Template “${result.name}” is now ${active ? "active" : "inactive"}.`,
    );
  }

  async function createDraftVersion() {
    const created = await createVersionMutation.mutateAsync();
    router.push(
      `/admin/templates/${templateId}/versions/${created.id}`,
    );
  }

  return (
    <div className="space-y-7">
      <Link
        href="/admin/templates"
        className={cn(
          buttonVariants({ variant: "ghost", size: "sm" }),
          "-ml-2",
        )}
      >
        <ArrowLeft aria-hidden="true" />
        Back to template management
      </Link>

      <PageHeader
        title={template.name}
        description={`${template.category.name} · Created ${formatDate(template.createdAt)}`}
        actions={
          <>
            {template.status === "Draft" ? (
              <Button
                type="button"
                variant="outline"
                disabled={categoriesQuery.isPending || categoriesQuery.isError}
                onClick={() => {
                  updateMutation.reset();
                  setEditOpen(true);
                }}
              >
                <Pencil aria-hidden="true" />
                Edit metadata
              </Button>
            ) : null}
            <Button
              type="button"
              variant={template.status === "Active" ? "outline" : "default"}
              disabled={stateMutation.isPending || activationDisabled}
              title={
                activationDisabled
                  ? "A current Published version is required before activation."
                  : undefined
              }
              onClick={() => changeState(template.id, template.status)}
            >
              {stateMutation.isPending
                ? "Saving…"
                : template.status === "Active"
                  ? "Deactivate"
                  : "Activate"}
            </Button>
          </>
        }
      />

      {successMessage ? (
        <Alert role="status">
          <CheckCircle2 aria-hidden="true" />
          <AlertTitle>Changes saved</AlertTitle>
          <AlertDescription>{successMessage}</AlertDescription>
        </Alert>
      ) : null}

      {stateMutation.isError ? (
        <Alert variant="destructive" role="alert">
          <AlertTitle>Template status could not be changed</AlertTitle>
          <AlertDescription>
            {getApiErrorMessage(
              stateMutation.error,
              "Review the current version state and try again.",
            )}
          </AlertDescription>
        </Alert>
      ) : null}

      {createVersionMutation.isError ? (
        <Alert variant="destructive" role="alert">
          <AlertTitle>Draft version could not be created</AlertTitle>
          <AlertDescription>
            {getApiErrorMessage(
              createVersionMutation.error,
              "Review the template state and try again.",
            )}
          </AlertDescription>
        </Alert>
      ) : null}

      {activationDisabled ? (
        <Alert>
          <AlertTitle>Activation is not available yet</AlertTitle>
          <AlertDescription>
            Publish a version and set it as Current before activating this
            template.
          </AlertDescription>
        </Alert>
      ) : null}

      <section
        aria-labelledby="template-overview-heading"
        className="rounded-xl border bg-card"
      >
        <div className="border-b px-5 py-4">
          <h2 id="template-overview-heading" className="font-semibold">
            Template overview
          </h2>
        </div>
        <dl className="grid gap-px bg-border sm:grid-cols-3">
          <div className="bg-card px-5 py-4">
            <dt className="text-xs font-medium text-muted-foreground">
              Status
            </dt>
            <dd className="mt-2">
              <AdminStatusBadge
                label={template.status}
                tone={statusTone(template.status)}
              />
            </dd>
          </div>
          <div className="bg-card px-5 py-4">
            <dt className="text-xs font-medium text-muted-foreground">
              Category
            </dt>
            <dd className="mt-2 text-sm font-medium">
              {template.category.name}
            </dd>
          </div>
          <div className="bg-card px-5 py-4">
            <dt className="text-xs font-medium text-muted-foreground">
              Versions
            </dt>
            <dd className="mt-2 text-sm font-medium tabular-nums">
              {versions.length}
            </dd>
          </div>
        </dl>
      </section>

      <section
        aria-labelledby="versions-heading"
        className="overflow-hidden rounded-xl border bg-card"
      >
        <div className="flex flex-col gap-3 border-b px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 id="versions-heading" className="font-semibold">
              Versions
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Draft new revisions from the Current or latest version.
            </p>
          </div>
          <Button
            type="button"
            size="sm"
            disabled={createVersionMutation.isPending}
            onClick={createDraftVersion}
          >
            {createVersionMutation.isPending ? (
              <LoaderCircle aria-hidden="true" className="animate-spin" />
            ) : (
              <Plus aria-hidden="true" />
            )}
            {createVersionMutation.isPending ? "Creating…" : "Create Draft version"}
          </Button>
        </div>
        {versionsQuery.isError ? (
          <div className="p-4">
            <ErrorState
              title="Versions are unavailable"
              description={getApiErrorMessage(
                versionsQuery.error,
                "We couldn’t load the template versions.",
              )}
              onRetry={() => versionsQuery.refetch()}
            />
          </div>
        ) : versions.length === 0 ? (
          <div className="p-4">
            <EmptyState
              title="No versions"
              description="This template does not have a version record."
            />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[42rem] text-left text-sm">
              <thead className="bg-muted/55 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
                <tr>
                  <th scope="col" className="px-5 py-3">
                    Version
                  </th>
                  <th scope="col" className="px-5 py-3">
                    Status
                  </th>
                  <th scope="col" className="px-5 py-3">
                    Format
                  </th>
                  <th scope="col" className="px-5 py-3">
                    Placeholders
                  </th>
                  <th scope="col" className="px-5 py-3">
                    Updated
                  </th>
                  <th scope="col" className="px-5 py-3 text-right">
                    <span className="sr-only">Open</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {versions.map((version) => (
                  <tr key={version.id} className="hover:bg-muted/35">
                    <td className="px-5 py-4 font-medium tabular-nums">
                      Version {version.versionNumber}
                      {version.isCurrent ? (
                        <span className="ml-2">
                          <AdminStatusBadge label="Current" tone="positive" />
                        </span>
                      ) : null}
                    </td>
                    <td className="px-5 py-4">
                      <AdminStatusBadge
                        label={version.status}
                        tone={
                          version.status === "Published"
                            ? "positive"
                            : "warning"
                        }
                      />
                    </td>
                    <td className="px-5 py-4 text-muted-foreground">
                      {version.contentFormat}
                    </td>
                    <td className="px-5 py-4 tabular-nums text-muted-foreground">
                      {version.placeholderCount}
                    </td>
                    <td className="px-5 py-4 tabular-nums text-muted-foreground">
                      {formatDate(version.updatedAt)}
                    </td>
                    <td className="px-5 py-4 text-right">
                      <Link
                        href={`/admin/templates/${template.id}/versions/${version.id}`}
                        className={buttonVariants({ variant: "ghost", size: "icon-sm" })}
                        aria-label={`Open version ${version.versionNumber}`}
                        title={`Open version ${version.versionNumber}`}
                      >
                        <ArrowRight aria-hidden="true" />
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <TemplateFormDialog
        open={editOpen}
        title="Edit Draft metadata"
        description="Change the name or category. Template content and version data remain untouched."
        submitLabel="Save changes"
        categories={categories}
        initialValues={initialValues}
        isPending={updateMutation.isPending}
        error={updateMutation.error ?? categoriesQuery.error}
        onOpenChange={setEditOpen}
        onSubmit={update}
      />
    </div>
  );
}
