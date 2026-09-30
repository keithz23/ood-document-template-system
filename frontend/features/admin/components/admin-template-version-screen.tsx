"use client";

import Link from "next/link";
import {
  ArrowLeft,
  CheckCircle2,
  CircleAlert,
  LoaderCircle,
  Pencil,
  Plus,
  Save,
  Trash2,
} from "lucide-react";
import { useState } from "react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Textarea } from "@/components/ui/textarea";
import { ErrorState, PageLoadingState } from "@/components/shared/data-state";
import { PageHeader } from "@/components/shared/page-header";
import {
  useAdminPlaceholders,
  useAdminTemplate,
  useAdminTemplateVersion,
  useCreateAdminPlaceholder,
  usePublishTemplateVersion,
  useRemoveAdminPlaceholder,
  useSetCurrentTemplateVersion,
  useUpdateAdminPlaceholder,
  useUpdateDraftTemplateVersion,
} from "@/features/admin/api/admin-queries";
import { AdminStatusBadge } from "@/features/admin/components/admin-status-badge";
import { PlaceholderFormDialog } from "@/features/admin/components/placeholder-form-dialog";
import { cn } from "@/lib/utils";
import { getApiErrorMessage } from "@/services/api-errors";
import type {
  CreatePlaceholderRequestDto,
  PlaceholderDto,
} from "@/types/api";

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}

export function AdminTemplateVersionScreen({
  templateId,
  versionId,
}: {
  templateId: string;
  versionId: string;
}) {
  const templateQuery = useAdminTemplate(templateId);
  const versionQuery = useAdminTemplateVersion(versionId);
  const placeholdersQuery = useAdminPlaceholders(versionId);
  const updateVersion = useUpdateDraftTemplateVersion(versionId);
  const publishVersion = usePublishTemplateVersion();
  const setCurrent = useSetCurrentTemplateVersion();
  const createPlaceholder = useCreateAdminPlaceholder(versionId);
  const updatePlaceholder = useUpdateAdminPlaceholder(versionId);
  const removePlaceholder = useRemoveAdminPlaceholder(versionId);
  const [contentDraft, setContentDraft] = useState<string | null>(null);
  const [publishOpen, setPublishOpen] = useState(false);
  const [placeholderOpen, setPlaceholderOpen] = useState(false);
  const [editingPlaceholder, setEditingPlaceholder] =
    useState<PlaceholderDto | null>(null);
  const [removingPlaceholder, setRemovingPlaceholder] =
    useState<PlaceholderDto | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const version = versionQuery.data;
  const template = templateQuery.data;
  const placeholders = placeholdersQuery.data ?? [];

  if (versionQuery.isPending || templateQuery.isPending) {
    return <PageLoadingState label="Loading template version" />;
  }

  if (versionQuery.isError || templateQuery.isError || !version || !template) {
    return (
      <ErrorState
        title="Template version is unavailable"
        description={getApiErrorMessage(
          versionQuery.error ?? templateQuery.error,
          "We couldn’t load this template version. Check the address and try again.",
        )}
        onRetry={() => {
          versionQuery.refetch();
          templateQuery.refetch();
        }}
      />
    );
  }

  const isDraft = version.status === "Draft";
  const content = contentDraft ?? version.content;
  const contentChanged = contentDraft !== null && content !== version.content;
  const placeholderMutationError =
    createPlaceholder.error ?? updatePlaceholder.error;

  async function saveContent() {
    const result = await updateVersion.mutateAsync({ content });
    setContentDraft(null);
    setSuccessMessage(`Version ${result.versionNumber} content was saved.`);
  }

  async function publish() {
    const result = await publishVersion.mutateAsync(versionId);
    setPublishOpen(false);
    setSuccessMessage(
      `Version ${result.versionNumber} is Published. It has not been made Current.`,
    );
  }

  async function makeCurrent() {
    const result = await setCurrent.mutateAsync(versionId);
    setSuccessMessage(`Version ${result.versionNumber} is now Current.`);
  }

  async function savePlaceholder(values: CreatePlaceholderRequestDto) {
    if (editingPlaceholder) {
      await updatePlaceholder.mutateAsync({
        placeholderId: editingPlaceholder.id,
        request: values,
      });
      setSuccessMessage(`Placeholder “${values.label}” was updated.`);
    } else {
      await createPlaceholder.mutateAsync(values);
      setSuccessMessage(`Placeholder “${values.label}” was added.`);
    }
    setPlaceholderOpen(false);
    setEditingPlaceholder(null);
  }

  async function removeSelectedPlaceholder() {
    if (!removingPlaceholder) return;
    await removePlaceholder.mutateAsync(removingPlaceholder.id);
    setSuccessMessage(`Placeholder “${removingPlaceholder.label}” was removed.`);
    setRemovingPlaceholder(null);
  }

  return (
    <div className="space-y-7">
      <Link
        href={`/admin/templates/${templateId}`}
        className={cn(buttonVariants({ variant: "ghost", size: "sm" }), "-ml-2")}
      >
        <ArrowLeft aria-hidden="true" />
        Back to {template.name}
      </Link>

      <PageHeader
        title={`Version ${version.versionNumber}`}
        description={`${template.name} · ${version.contentFormat} · Updated ${formatDate(version.updatedAt)}`}
        actions={
          <>
            <AdminStatusBadge
              label={version.status}
              tone={isDraft ? "warning" : "positive"}
            />
            {version.isCurrent ? (
              <AdminStatusBadge label="Current" tone="positive" />
            ) : null}
            {isDraft ? (
              <Button type="button" onClick={() => setPublishOpen(true)}>
                Publish
              </Button>
            ) : !version.isCurrent ? (
              <Button
                type="button"
                disabled={setCurrent.isPending}
                onClick={makeCurrent}
              >
                {setCurrent.isPending ? (
                  <LoaderCircle aria-hidden="true" className="animate-spin" />
                ) : null}
                {setCurrent.isPending ? "Setting…" : "Set Current"}
              </Button>
            ) : null}
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

      {updateVersion.isError || publishVersion.isError || setCurrent.isError ? (
        <Alert variant="destructive" role="alert">
          <CircleAlert aria-hidden="true" />
          <AlertTitle>Version could not be updated</AlertTitle>
          <AlertDescription>
            {getApiErrorMessage(
              updateVersion.error ?? publishVersion.error ?? setCurrent.error,
              "Review the version state and try again.",
            )}
          </AlertDescription>
        </Alert>
      ) : null}

      {!isDraft ? (
        <Alert>
          <CircleAlert aria-hidden="true" />
          <AlertTitle>Published version</AlertTitle>
          <AlertDescription>
            Content and placeholder definitions are read-only. Create a new Draft
            version to make changes.
          </AlertDescription>
        </Alert>
      ) : null}

      <section aria-labelledby="version-content-heading" className="border bg-card">
        <div className="flex flex-col gap-3 border-b px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 id="version-content-heading" className="font-semibold">
              Content
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
              HTML source for this version. Rich editing is not enabled yet.
            </p>
          </div>
          {isDraft ? (
            <Button
              type="button"
              size="sm"
              disabled={!contentChanged || updateVersion.isPending}
              onClick={saveContent}
            >
              {updateVersion.isPending ? (
                <LoaderCircle aria-hidden="true" className="animate-spin" />
              ) : (
                <Save aria-hidden="true" />
              )}
              {updateVersion.isPending ? "Saving…" : "Save content"}
            </Button>
          ) : null}
        </div>
        <div className="p-5">
          <Textarea
            value={content}
            disabled={!isDraft}
            onChange={(event) => setContentDraft(event.target.value)}
            aria-label="Template version content"
            className="min-h-64 resize-y font-mono leading-6"
          />
        </div>
      </section>

      <section aria-labelledby="placeholders-heading" className="border bg-card">
        <div className="flex flex-col gap-3 border-b px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 id="placeholders-heading" className="font-semibold">
              Placeholders
            </h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Values collected when users create documents from this version.
            </p>
          </div>
          {isDraft ? (
            <Button
              type="button"
              size="sm"
              onClick={() => {
                createPlaceholder.reset();
                updatePlaceholder.reset();
                setEditingPlaceholder(null);
                setPlaceholderOpen(true);
              }}
            >
              <Plus aria-hidden="true" />
              Add placeholder
            </Button>
          ) : null}
        </div>

        {placeholdersQuery.isPending ? (
          <div className="p-5 text-sm text-muted-foreground" aria-busy="true">
            Loading placeholders…
          </div>
        ) : placeholdersQuery.isError ? (
          <div className="p-4">
            <ErrorState
              title="Placeholders are unavailable"
              description={getApiErrorMessage(
                placeholdersQuery.error,
                "We couldn’t load the placeholders for this version.",
              )}
              onRetry={() => placeholdersQuery.refetch()}
            />
          </div>
        ) : placeholders.length === 0 ? (
          <div className="px-5 py-10 text-center">
            <p className="text-sm font-medium">No placeholders</p>
            <p className="mt-1 text-sm text-muted-foreground">
              This version does not collect structured values.
            </p>
          </div>
        ) : (
          <>
            <div className="hidden overflow-x-auto md:block">
              <table className="w-full min-w-[48rem] text-left text-sm">
                <thead className="bg-muted/55 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
                  <tr>
                    <th className="px-5 py-3" scope="col">Key</th>
                    <th className="px-5 py-3" scope="col">Label</th>
                    <th className="px-5 py-3" scope="col">Type</th>
                    <th className="px-5 py-3" scope="col">Required</th>
                    <th className="px-5 py-3" scope="col">Default</th>
                    <th className="px-5 py-3 text-right" scope="col">
                      <span className="sr-only">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {placeholders.map((placeholder) => (
                    <PlaceholderRow
                      key={placeholder.id}
                      placeholder={placeholder}
                      editable={isDraft}
                      onEdit={() => {
                        createPlaceholder.reset();
                        updatePlaceholder.reset();
                        setEditingPlaceholder(placeholder);
                        setPlaceholderOpen(true);
                      }}
                      onRemove={() => setRemovingPlaceholder(placeholder)}
                    />
                  ))}
                </tbody>
              </table>
            </div>
            <div className="divide-y md:hidden">
              {placeholders.map((placeholder) => (
                <div key={placeholder.id} className="space-y-3 px-4 py-4">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="break-words text-sm font-medium">{placeholder.label}</p>
                      <p className="mt-1 break-all font-mono text-xs text-muted-foreground">
                        {placeholder.key}
                      </p>
                    </div>
                    {isDraft ? (
                      <PlaceholderActions
                        label={placeholder.label}
                        onEdit={() => {
                          setEditingPlaceholder(placeholder);
                          setPlaceholderOpen(true);
                        }}
                        onRemove={() => setRemovingPlaceholder(placeholder)}
                      />
                    ) : null}
                  </div>
                  <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                    <AdminStatusBadge label={placeholder.dataType} tone="neutral" />
                    <span>{placeholder.isRequired ? "Required" : "Optional"}</span>
                    {placeholder.defaultValue !== null ? (
                      <span className="break-all">Default: {placeholder.defaultValue}</span>
                    ) : null}
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </section>

      <PlaceholderFormDialog
        open={placeholderOpen}
        placeholder={editingPlaceholder}
        isPending={createPlaceholder.isPending || updatePlaceholder.isPending}
        error={placeholderMutationError}
        onOpenChange={(open) => {
          setPlaceholderOpen(open);
          if (!open) setEditingPlaceholder(null);
        }}
        onSubmit={savePlaceholder}
      />

      <Dialog open={publishOpen} onOpenChange={setPublishOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Publish version {version.versionNumber}?</DialogTitle>
            <DialogDescription>
              Publishing makes its content and placeholders immutable. It will not
              become Current automatically.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              disabled={publishVersion.isPending}
              onClick={() => setPublishOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="button"
              disabled={publishVersion.isPending}
              onClick={publish}
            >
              {publishVersion.isPending ? (
                <LoaderCircle aria-hidden="true" className="animate-spin" />
              ) : null}
              {publishVersion.isPending ? "Publishing…" : "Publish version"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={Boolean(removingPlaceholder)}
        onOpenChange={(open) => {
          if (!open) setRemovingPlaceholder(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Remove placeholder?</DialogTitle>
            <DialogDescription>
              “{removingPlaceholder?.label}” will be removed from this Draft version.
            </DialogDescription>
          </DialogHeader>
          {removePlaceholder.isError ? (
            <Alert variant="destructive" role="alert">
              <AlertTitle>Placeholder could not be removed</AlertTitle>
              <AlertDescription>
                {getApiErrorMessage(removePlaceholder.error, "Try again.")}
              </AlertDescription>
            </Alert>
          ) : null}
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              disabled={removePlaceholder.isPending}
              onClick={() => setRemovingPlaceholder(null)}
            >
              Cancel
            </Button>
            <Button
              type="button"
              variant="destructive"
              disabled={removePlaceholder.isPending}
              onClick={removeSelectedPlaceholder}
            >
              {removePlaceholder.isPending ? (
                <LoaderCircle aria-hidden="true" className="animate-spin" />
              ) : (
                <Trash2 aria-hidden="true" />
              )}
              {removePlaceholder.isPending ? "Removing…" : "Remove"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function PlaceholderRow({
  placeholder,
  editable,
  onEdit,
  onRemove,
}: {
  placeholder: PlaceholderDto;
  editable: boolean;
  onEdit: () => void;
  onRemove: () => void;
}) {
  return (
    <tr className="hover:bg-muted/35">
      <td className="px-5 py-4 font-mono text-xs">{placeholder.key}</td>
      <td className="px-5 py-4 font-medium">{placeholder.label}</td>
      <td className="px-5 py-4">
        <AdminStatusBadge label={placeholder.dataType} tone="neutral" />
      </td>
      <td className="px-5 py-4 text-muted-foreground">
        {placeholder.isRequired ? "Required" : "Optional"}
      </td>
      <td className="max-w-48 truncate px-5 py-4 text-muted-foreground">
        {placeholder.defaultValue ?? "None"}
      </td>
      <td className="px-5 py-4 text-right">
        {editable ? (
          <PlaceholderActions
            label={placeholder.label}
            onEdit={onEdit}
            onRemove={onRemove}
          />
        ) : null}
      </td>
    </tr>
  );
}

function PlaceholderActions({
  label,
  onEdit,
  onRemove,
}: {
  label: string;
  onEdit: () => void;
  onRemove: () => void;
}) {
  return (
    <div className="inline-flex gap-1">
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        aria-label={`Edit ${label}`}
        title={`Edit ${label}`}
        onClick={onEdit}
      >
        <Pencil aria-hidden="true" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        aria-label={`Remove ${label}`}
        title={`Remove ${label}`}
        onClick={onRemove}
      >
        <Trash2 aria-hidden="true" />
      </Button>
    </div>
  );
}
